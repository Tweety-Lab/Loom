using Loom.Analyzer.Symbols;
using Loom.Common.Diagnostics;
using Loom.Parser.AST;
using Loom.Parser.AST.Rules.Default;

namespace Loom.Analyzer.Analyzers;

/// <summary>
/// Checks for infractions on mutability.
/// </summary>
[LoomAnalyzer]
public class MutabilityAnalyzer : Analyzer
{
    public static Diagnostic CannotMutateRef = new("LM0023", Diagnostic.DiagnosticLevel.Error, "Cannot mutate borrowed parameter '{0}' because it is not marked 'mut'.");
    public static Diagnostic CannotMutateConstant = new("LM0024", Diagnostic.DiagnosticLevel.Error, "Cannot mutate constant '{0}'.");

    [Visitor]
    public void Visit(AssignmentStatementNode node)
    {
        CheckConstant(node.Target);
        if (FindBorrowedParameter(node.Target) is not { IsMutable: false } parameter)
            return;

        Context.DiagnosticContext?.Report(CannotMutateRef, node.Span.StartToken?.Location, parameter.Name);
    }

    [Visitor]
    public void Visit(IncrementExpressionNode node)
    {
        CheckConstant(node.Target);
        if (FindBorrowedParameter(node.Target) is not { IsMutable: false } parameter)
            return;

        Context.DiagnosticContext?.Report(CannotMutateRef, node.Span.StartToken?.Location, parameter.Name);
    }

    private void CheckConstant(ExpressionNode target)
    {
        if (Context.GetSymbol(target).Symbol is FieldSymbol { IsConstant: true } field)
            Context.DiagnosticContext?.Report(CannotMutateConstant, target.Span.StartToken?.Location, field.Name);
    }

    private ParameterSymbol? FindBorrowedParameter(ExpressionNode target) => target switch
    {
        IdentifierNameNode identifier => Context.GetSymbol(identifier).Symbol is ParameterSymbol { IsBorrow: true } parameter ? parameter : null,
        MemberAccessExpressionNode member => FindBorrowedParameter(member.Receiver),
        ArrayAccessExpressionNode array => FindBorrowedParameter(array.Receiver),
        _ => null
    };
}
