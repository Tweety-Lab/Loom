using Loom.Analyzer.Symbols;
using Loom.Common.Diagnostics;
using Loom.Parser.AST;
using Loom.Parser.AST.Rules.Default;
using Loom.Parser.Tokenizer;

namespace Loom.Analyzer.Analyzers;

/// <summary>
/// Checks for usage of stack-only types that would violate stack-only-ness.
/// </summary>
[LoomAnalyzer]
public class StackOnlyTypeAnalyzer : Analyzer
{
    public static Diagnostic FieldCannotBeStackOnlyType = new(Diagnostic.DiagnosticLevel.Error, "Field cannot be of type '{0}' because it is a stack-only type.");
    public static Diagnostic TypeParameterCannotBeStackOnlyType = new(Diagnostic.DiagnosticLevel.Error, "Type parameter cannot be of type '{0}' because it is a stack-only type.");


    [Visitor]
    public void Visit(FieldDeclarationNode node)
    {
        FieldSymbol? symbol = Context.GetSymbol(node).Symbol as FieldSymbol;
        if (symbol == null)
            return;

        if (symbol.Type?.IsStackOnly == true)
            Context.DiagnosticContext?.Report(FieldCannotBeStackOnlyType, node.Variable.StartToken?.Location, symbol.Type.Name);
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
                Context.DiagnosticContext?.Report(TypeParameterCannotBeStackOnlyType, typeArg.StartToken?.Location, symbol.Name);
        }
    }

}
