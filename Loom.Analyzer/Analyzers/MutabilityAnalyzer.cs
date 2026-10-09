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
    public static Diagnostic CannotMutateRef = new(Diagnostic.DiagnosticLevel.Error, "Cannot mutate borrowed parameter '{0}' because it is not marked 'mut'.");

    [Visitor]
    public void Visit(AssignmentStatementNode node)
    {
        if (FindBorrowedParameter(node.Target) is not { IsMutable: false } parameter)
            return;

        Context.DiagnosticContext?.Report(CannotMutateRef, node.StartToken?.Location, parameter.Name);
    }

    private ParameterSymbol? FindBorrowedParameter(ExpressionNode target) => target switch
    {
        IdentifierNameNode identifier => Context.GetSymbol(identifier).Symbol is ParameterSymbol { IsBorrow: true } parameter ? parameter : null,
        MemberAccessExpressionNode member => FindBorrowedParameter(member.Receiver),
        ArrayAccessExpressionNode array => FindBorrowedParameter(array.Receiver),
        _ => null
    };
}