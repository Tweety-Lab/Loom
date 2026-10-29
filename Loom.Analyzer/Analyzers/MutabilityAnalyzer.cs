using static Loom.Common.Diagnostics.DiagnosticDescription;
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
    public static DiagnosticDescription CannotMutateReference = new($"LM{Code:D4}", Diagnostic.DiagnosticLevel.Error, "Cannot mutate borrowed reference '{0}' because it is not marked 'mut'.");
    public static DiagnosticDescription CannotMutateConstant = new($"LM{Code:D4}", Diagnostic.DiagnosticLevel.Error, "Cannot mutate constant '{0}'.");

    [Visitor]
    public void Visit(AssignmentStatementNode node) => CheckMutation(node.Target);

    [Visitor]
    public void Visit(IncrementExpressionNode node) => CheckMutation(node.Target);

    private void CheckMutation(ExpressionNode target)
    {
        CheckConstant(target);
        if (FindImmutableReference(target) is not { } reference)
            return;

        Context.DiagnosticContext?.Report(CannotMutateReference, target.Span.StartToken?.Location, reference.Name);
    }

    private void CheckConstant(ExpressionNode target)
    {
        if (Context.GetSymbol(target).Symbol is FieldSymbol { IsConstant: true } field)
            Context.DiagnosticContext?.Report(CannotMutateConstant, target.Span.StartToken?.Location, field.Name);
    }

    private Symbol? FindImmutableReference(ExpressionNode target)
    {
        var symbol = Context.GetSymbol(target).Symbol;
        if (symbol is ParameterSymbol { IsBorrow: true, IsMutable: false }
            or LocalVariableSymbol { PointerType: PointerType.Ref, IsMutable: false }
            or FieldSymbol { PointerType: PointerType.Ref, IsMutable: false }
            or PropertySymbol { PointerType: PointerType.Ref, IsMutable: false }
            || target is CallExpressionNode && symbol is MethodSymbol { ReturnPointerType: PointerType.Ref, ReturnIsMutable: false })
            return symbol;

        return target switch
        {
            MemberAccessExpressionNode member => FindImmutableReference(member.Receiver),
            ArrayAccessExpressionNode array => FindImmutableReference(array.Receiver),
            _ => null
        };
    }
}
