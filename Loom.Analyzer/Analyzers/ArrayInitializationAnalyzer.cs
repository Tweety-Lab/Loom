using Loom.Analyzer.Symbols;
using Loom.Common.Diagnostics;
using Loom.Parser.AST;
using Loom.Parser.AST.Rules.Default;

namespace Loom.Analyzer.Analyzers;

/// <summary>
/// Ensures arrays are initialized.
/// </summary>
[LoomAnalyzer]
public class ArrayInitializationAnalyzer : Analyzer
{
    public static Diagnostic DefaultInitializerRequired = new(Diagnostic.DiagnosticLevel.Error, "Arrays must be initialized with 'default'.");

    [Visitor]
    public void Visit(LocalDeclarationStatementNode node)
    {
        if (Context.GetSymbol(node).Symbol is LocalVariableSymbol { Type: ArrayTypeSymbol })
            Validate(node.Variable);
    }

    [Visitor]
    public void Visit(FieldDeclarationNode node)
    {
        if (Context.GetSymbol(node).Symbol is FieldSymbol { Type: ArrayTypeSymbol })
            Validate(node.Variable);
    }

    [Visitor]
    public void Visit(AssignmentStatementNode node)
    {
        if (Context.ExpressionTypes.TryGetValue(node.Target, out var target) && target is ArrayTypeSymbol)
            Validate(node.Value);
    }

    private void Validate(VariableDeclarationNode declaration)
    {
        if (declaration.Initializer is not DefaultLiteralNode)
            Context.DiagnosticContext?.Report(DefaultInitializerRequired, declaration.Initializer.StartToken?.Location);
    }

    private void Validate(ExpressionNode initializer)
    {
        if (initializer is not DefaultLiteralNode)
            Context.DiagnosticContext?.Report(DefaultInitializerRequired, initializer.StartToken?.Location);
    }
}
