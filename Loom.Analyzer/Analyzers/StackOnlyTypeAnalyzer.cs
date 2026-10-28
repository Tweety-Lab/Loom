using Loom.Analyzer.Symbols;
using Loom.Common.Diagnostics;
using Loom.Parser.AST;
using Loom.Parser.AST.Rules.Default;

namespace Loom.Analyzer.Analyzers;

/// <summary>
/// Checks for usage of stack-only types that would violate stack-only-ness.
/// </summary>
[LoomAnalyzer]
public class StackOnlyTypeAnalyzer : Analyzer
{
    public static Diagnostic FieldCannotBeStackOnlyType = new("LM0027", Diagnostic.DiagnosticLevel.Error, "Field cannot be of type '{0}' because it is a stack-only type.");
    public static Diagnostic TypeParameterCannotBeStackOnlyType = new("LM0028", Diagnostic.DiagnosticLevel.Error, "Type parameter cannot be of type '{0}' because it is a stack-only type.");


    [Visitor]
    public void Visit(FieldDeclarationNode node)
    {
        FieldSymbol? symbol = Context.GetSymbol(node).Symbol as FieldSymbol;
        if (symbol == null)
            return;

        if (symbol.Type?.IsStackOnly == true)
            Context.DiagnosticContext?.Report(FieldCannotBeStackOnlyType, node.Variable.Span.StartToken?.Location, symbol.Type.Name);
    }

    [Visitor]
    public void Visit(CallExpressionNode node)
    {
        foreach (var typeArg in node.TypeArguments)
        {
            TypeSymbol? symbol = Context.GetSymbol(typeArg).Symbol as TypeSymbol;
            if (symbol == null)
                continue;

            if (symbol.IsStackOnly)
                Context.DiagnosticContext?.Report(TypeParameterCannotBeStackOnlyType, typeArg.Span.StartToken?.Location, symbol.Name);
        }
    }

}
