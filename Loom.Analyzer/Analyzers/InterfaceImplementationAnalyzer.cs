using Loom.Analyzer.Symbols;
using Loom.Common.Diagnostics;
using Loom.Parser.AST;
using Loom.Parser.AST.Rules.Default;

namespace Loom.Analyzer.Analyzers;

/// <summary>
/// Checks types that implement interfaces implement members from those interfaces.
/// </summary>
[LoomAnalyzer]
public class InterfaceImplementationAnalyzer : Analyzer
{
    public static Diagnostic MissingImplementation = new(Diagnostic.DiagnosticLevel.Error, "Type '{0}' does not implement interface member '{1}'.");

    [Visitor]
    public void Visit(ClassDeclarationNode node) => Check(node);

    [Visitor]
    public void Visit(StructDeclarationNode node) => Check(node);

    private void Check(ASTNode node)
    {
        if (Context.GetSymbol(node).Symbol is not TypeSymbol type)
            return;

        var members = ImplementationMembers(type).ToArray();
        var contracts = type.Interfaces.SelectMany(contract => contract.AllInterfaces.Prepend(contract)).Distinct();

        foreach (var contract in contracts)
        {
            foreach (var required in contract.Members)
            {
                bool? implemented = required switch
                {
                    MethodSymbol { Kind: MethodSymbol.MethodKind.Method, Accessibility: MemberAccessibility.Public } method
                        when method.ReturnType != null && method.Parameters.All(parameter => parameter.Type != null)
                        => members.OfType<MethodSymbol>().Any(candidate => Implements(candidate, method)),
                    PropertySymbol { Accessibility: MemberAccessibility.Public, Type: not null } property
                        => members.OfType<PropertySymbol>().Any(candidate => Implements(candidate, property)),
                    _ => null
                };

                if (implemented == false)
                    Context.DiagnosticContext?.Report(MissingImplementation, node.StartToken?.Location, type.FullyQualifiedName, Display(required));
            }
        }
    }

    /// <summary> Enumerates potential implementations, starting with the most derived type. </summary>
    public static IEnumerable<Symbol> ImplementationMembers(TypeSymbol type)
    {
        var visited = new HashSet<TypeSymbol>();
        for (TypeSymbol? current = type; current != null && visited.Add(current.OriginalDefinition); current = current.BaseType)
            foreach (var member in current.Members)
                yield return member;
    }

    /// <summary> Whether a method satisfies an interface signature, including generic and borrow modifiers. </summary>
    public static bool Implements(MethodSymbol candidate, MethodSymbol required)
    {
        if (candidate.Kind != required.Kind || candidate.Name != required.Name ||
            candidate.Accessibility != MemberAccessibility.Public || candidate.IsStatic != required.IsStatic ||
            candidate.Arity != required.Arity || candidate.Parameters.Count != required.Parameters.Count ||
            candidate.ReturnPointerType != required.ReturnPointerType || candidate.ReturnType == null)
            return false;

        var substitution = new TypeSubstitution(required.TypeParameters.Select((parameter, index) => KeyValuePair.Create(parameter, (TypeSymbol)candidate.TypeParameters[index])));
        if (!TypeConversions.IsIdentical(candidate.ReturnType, substitution.Resolve(required.ReturnType!)))
            return false;

        return candidate.Parameters.Zip(required.Parameters).All(pair =>
            pair.First.Type is { } actual && pair.Second.Type is { } expected &&
            TypeConversions.IsIdentical(actual, substitution.Resolve(expected)) &&
            pair.First.IsBorrow == pair.Second.IsBorrow &&
            (!pair.First.IsBorrow || pair.First.IsMutable == pair.Second.IsMutable));
    }

    /// <summary> Whether a property satisfies an interface's type and accessor requirements. </summary>
    public static bool Implements(PropertySymbol candidate, PropertySymbol required) =>
        candidate.Name == required.Name && candidate.Accessibility == MemberAccessibility.Public &&
        candidate.IsStatic == required.IsStatic && candidate.Type is { } type && TypeConversions.IsIdentical(type, required.Type!) &&
        (required.Getter == null || candidate.Getter is { Accessibility: MemberAccessibility.Public }) &&
        (required.Setter == null || candidate.Setter is { Accessibility: MemberAccessibility.Public });

    private static string Display(Symbol member)
    {
        if (member is not MethodSymbol method)
            return member.FullyQualifiedName;

        var typeParameters = method.Arity == 0 ? string.Empty : $"<{string.Join(", ", method.TypeParameters.Select(parameter => parameter.Name))}>";
        var parameters = method.Parameters.Select(parameter =>
        {
            var modifier = parameter.IsBorrow ? parameter.IsMutable ? "ref mut " : "ref " : string.Empty;
            return $"{modifier}{parameter.Type!.FullyQualifiedName}";
        });
        return $"{method.FullyQualifiedName}{typeParameters}({string.Join(", ", parameters)})";
    }
}
