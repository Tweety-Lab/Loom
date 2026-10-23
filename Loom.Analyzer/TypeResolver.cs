using Loom.Analyzer.Symbols;
using Loom.Common.Diagnostics;
using Loom.Parser.AST;
using Loom.Parser.AST.Rules.Default;

namespace Loom.Analyzer;

/// <summary>
/// Resolves type names within the current scope, falling back to types exported by imported modules.
/// </summary>
internal static class TypeResolver
{
    private static readonly Diagnostic WrongArity = new(Diagnostic.DiagnosticLevel.Error, "Type '{0}' expects {1} type argument(s), got {2}.");

    /// <summary> Resolves a type by name in the scope of <paramref name="node"/>, or null if it could not be found. </summary>
    public static TypeSymbol? Resolve(AnalysisContext context, ASTNode node, string name)
    {
        if (context.GetBinder(node)?.Lookup(name)?.FirstOrDefault(s => s is TypeSymbol) is TypeSymbol inScope)
            return inScope;

        var programNode = context.FirstAncestorOrSelf<ProgramNode>(node);
        if (programNode == null)
            return null;

        foreach (var import in programNode.Imports)
        {
            var moduleSymbol = context.GetSymbol(import.ModuleName).Symbol as ModuleSymbol;
            if (moduleSymbol?.DeclaringNode is not ModuleNode moduleNode)
                continue;

            if (context.Binders.TryGetValue(moduleNode, out var moduleBinder)
                && moduleBinder.Lookup(name)?.FirstOrDefault(s => s is TypeSymbol) is TypeSymbol imported)
                return imported;
        }

        return null;
    }

    /// <summary> Resolves a declared type. </summary>
    public static TypeSymbol? Resolve(AnalysisContext context, ASTNode node, TypeNode type)
    {
        var elementType = Resolve(context, node, type.Base.Text);

        if (elementType == null)
            return null;

        if (type.TypeArguments is { Count: > 0 } typeArguments)
        {
            elementType = Construct(context, node, elementType, typeArguments);
            if (elementType == null)
                return null;
        }

        var resolved = type.ArraySize?.Size is { Text: var size } && int.TryParse(size, out var arraySize) ? new ArrayTypeSymbol(elementType, arraySize) : elementType;

        context.Bind(type, resolved);
        return resolved;
    }

    /// <summary> Resolves arguments in their use-site scope, then constructs the semantic type. </summary>
    internal static TypeSymbol? Construct(AnalysisContext context, ASTNode node, TypeSymbol definition, IReadOnlyList<TypeNode> typeArguments)
    {
        var arguments = typeArguments.Select(argument => Resolve(context, node, argument)).ToArray();
        if (definition.Arity != arguments.Length)
        {
            context.DiagnosticContext?.Report(WrongArity, node.Span.StartToken?.Location, definition.Name, definition.Arity, arguments.Length);
            return null;
        }
        return arguments.Any(argument => argument == null) ? null : definition.Construct([.. arguments!]);
    }
}
