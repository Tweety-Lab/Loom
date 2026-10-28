using Loom.Analyzer.Analyzers;
using Loom.Analyzer.Symbols;
using Loom.Common.Diagnostics;
using Loom.Parser.AST;
using Loom.Parser.AST.Rules.Default;

namespace Loom.Analyzer;

internal static class InheritanceBinder
{
    private static readonly Diagnostic InvalidBase = new("LM0042", Diagnostic.DiagnosticLevel.Error, "Type '{0}' cannot inherit from '{1}'.");
    private static readonly Diagnostic DuplicateInterface = new("LM0043", Diagnostic.DiagnosticLevel.Error, "Interface '{0}' is listed more than once on type '{1}'.");
    private static readonly Diagnostic BaseClassOrder = new("LM0044", Diagnostic.DiagnosticLevel.Error, "Base class '{0}' must be declared before interfaces on type '{1}'.");
    private static readonly Diagnostic WrongArity = new("LM0045", Diagnostic.DiagnosticLevel.Error, "Type '{0}' expects {1} type argument(s), got {2}.");
    private static readonly Diagnostic CircularInheritance = new("LM0046", Diagnostic.DiagnosticLevel.Error, "Inheritance cycle involving type '{0}'.");

    public static void Bind(AnalysisContext context, IEnumerable<ProgramNode> roots)
    {
        var declarations = roots.SelectMany(root => root.Descendants).Where(node => node is IInheritableNode).ToArray();

        foreach (var declaration in declarations)
        {
            if (context.GetSymbol(declaration).Symbol is not TypeSymbol owner)
                continue;

            var interfaces = new List<TypeSymbol>();
            var inheritance = ((IInheritableNode)declaration).Inheritance;
            owner.BaseType = null;

            foreach (var syntax in inheritance)
            {
                if (Resolve(context, syntax) is not { } type)
                    continue;

                if (owner.IsStatic || type.IsStatic || syntax.Modifiers.Count != 0 || syntax.ArraySize != null)
                    context.DiagnosticContext?.Report(InvalidBase, syntax.Span.StartToken?.Location, owner.Name, type.FullyQualifiedName);
                else if (type.KnownType == TypeSymbol.DefaultType.Interface)
                {
                    if (interfaces.Contains(type))
                        context.DiagnosticContext?.Report(DuplicateInterface, syntax.Span.StartToken?.Location, type.FullyQualifiedName, owner.Name);
                    else
                        interfaces.Add(type);
                }
                else if (owner.KnownType == TypeSymbol.DefaultType.Class && type.KnownType == TypeSymbol.DefaultType.Class && owner.BaseType == null)
                {
                    owner.BaseType = type;
                    if (syntax != inheritance[0])
                        context.DiagnosticContext?.Report(BaseClassOrder, syntax.Span.StartToken?.Location, type.FullyQualifiedName, owner.Name);
                }
                else
                    context.DiagnosticContext?.Report(InvalidBase, syntax.Span.StartToken?.Location, owner.Name, type.FullyQualifiedName);
            }

            owner.Interfaces = interfaces.ToArray();
        }

        foreach (var declaration in declarations)
        {
            if (context.GetSymbol(declaration).Symbol is TypeSymbol owner && HasCycle(owner, owner, []))
                context.DiagnosticContext?.Report(CircularInheritance, declaration.Span.StartToken?.Location, owner.Name);
        }
    }

    private static TypeSymbol? Resolve(AnalysisContext context, TypeNode syntax)
    {
        var definition = TypeResolver.Resolve(context, syntax, syntax.Base.Text);
        if (definition == null)
        {
            context.DiagnosticContext?.Report(UnresolvedTypeAnalyzer.UnresolvedType, syntax.Base.Location, syntax.Base.Text);
            return null;
        }

        var arguments = syntax.TypeArguments?.Select(argument => Resolve(context, argument)).ToArray() ?? [];
        if (definition.Arity != arguments.Length)
        {
            context.DiagnosticContext?.Report(WrongArity, syntax.Base.Location, definition.Name, definition.Arity, arguments.Length);
            return null;
        }

        if (arguments.Any(argument => argument == null))
            return null;

        var resolved = arguments.Length == 0 ? definition : definition.Construct([.. arguments!]);
        if (syntax.ArraySize?.Size is { Text: var sizeText } && int.TryParse(sizeText, out var size))
            resolved = new ArrayTypeSymbol(resolved, size);

        context.Bind(syntax, resolved);
        return resolved;
    }

    private static bool HasCycle(TypeSymbol owner, TypeSymbol current, HashSet<TypeSymbol> visited)
    {
        current = current.OriginalDefinition;
        if (!visited.Add(current))
            return false;

        IEnumerable<TypeSymbol> bases = current.Interfaces;
        if (current.BaseType is { } baseType)
            bases = bases.Prepend(baseType);

        return bases.Any(type => ReferenceEquals(type.OriginalDefinition, owner) || HasCycle(owner, type, visited));
    }
}
