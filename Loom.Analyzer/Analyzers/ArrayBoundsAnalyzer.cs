using static Loom.Common.Diagnostics.DiagnosticDescription;
using Loom.Analyzer.Symbols;
using Loom.Common.Diagnostics;
using Loom.Parser.AST;
using Loom.Parser.AST.Rules.Default;

namespace Loom.Analyzer.Analyzers;

/// <summary>
/// Validates array indexing.
/// </summary>
[LoomAnalyzer]
public class ArrayBoundsAnalyzer : Analyzer
{
    public static DiagnosticDescription NonArrayIndex = new($"LM{Code:D4}", Diagnostic.DiagnosticLevel.Error, "Cannot index '{0}' because it is not an array.");
    public static DiagnosticDescription InvalidIndexType = new($"LM{Code:D4}", Diagnostic.DiagnosticLevel.Error, "Array indexes must be i32 values.");
    public static DiagnosticDescription IndexOutOfBounds = new($"LM{Code:D4}", Diagnostic.DiagnosticLevel.Error, "Array index {0} is outside the bounds of array '{1}' of size {2}.");
    public static DiagnosticDescription TooManyElements = new($"LM{Code:D4}", Diagnostic.DiagnosticLevel.Error, "Array literal with {0} elements is outside the bounds of array '{1}' of size {2}.");
    public static DiagnosticDescription IndexOperatorResultNotAssignable = new($"LM{Code:D4}", Diagnostic.DiagnosticLevel.Error, "The result of operator[] cannot be assigned to.");

    [Visitor]
    public void Visit(ArrayAccessExpressionNode node)
    {
        var name = Describe(node.Receiver);

        if (!Context.ExpressionTypes.TryGetValue(node.Receiver, out var receiverType) || receiverType is not ArrayTypeSymbol array)
        {
            if (OverloadResolver.DiagnoseIndexOperator(Context, node) is { } failure)
            {
                var diagnostic = failure == OverloadFailure.Ambiguous ? UnresolvedOverloadAnalyzer.AmbiguousOverload : UnresolvedOverloadAnalyzer.NoMatchingOverload;
                Context.DiagnosticContext?.Report(diagnostic, node.Span.StartToken?.Location ?? node.Receiver.Span.StartToken?.Location, "operator[]", 2);
                return;
            }

            if (Context.GetSymbol(node).Symbol is MethodSymbol { Kind: MethodSymbol.MethodKind.Operator })
            {
                if (Context.GetParent(node) is AssignmentStatementNode assignment && assignment.Target == node || Context.GetParent(node) is IncrementExpressionNode increment && increment.Target == node)
                    Context.DiagnosticContext?.Report(IndexOperatorResultNotAssignable, node.Span.StartToken?.Location ?? node.Receiver.Span.StartToken?.Location);

                return;
            }

            Context.DiagnosticContext?.Report(NonArrayIndex, node.Receiver.Span.StartToken?.Location, name);
            return;
        }

        if (!Context.ExpressionTypes.TryGetValue(node.Index, out var indexType) || indexType.KnownType != TypeSymbol.DefaultType.I32)
        {
            Context.DiagnosticContext?.Report(InvalidIndexType, node.Index.Span.StartToken?.Location);
            return;
        }

        if (node.Index is NumberLiteralNode literal && literal.Value >= array.Size)
            Context.DiagnosticContext?.Report(IndexOutOfBounds, literal.Span.StartToken?.Location, literal.Value, name, array.Size);
    }

    [Visitor]
    public void Visit(ArrayLiteralNode node)
    {
        if (!Context.ExpressionTypes.TryGetValue(node, out var type) || type is not ArrayTypeSymbol array)
            return;

        if (node.Elements.Count > array.Size)
            Context.DiagnosticContext?.Report(TooManyElements, node.Span.StartToken?.Location, node.Elements.Count, array.Name, array.Size);
    }

    private static string Describe(ExpressionNode node) => node switch
    {
        IdentifierNameNode identifier => identifier.BaseName,
        MemberAccessExpressionNode member => $"{Describe(member.Receiver)}.{member.Name.BaseName}",
        ArrayAccessExpressionNode array => $"{Describe(array.Receiver)}[...]",
        _ => node.ToString() ?? string.Empty
    };
}
