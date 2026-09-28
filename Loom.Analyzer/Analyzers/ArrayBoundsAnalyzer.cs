using Loom.Analyzer.Symbols;
using Loom.Common.Diagnostics;
using Loom.Parser.AST;
using Loom.Parser.Rules.Default;

namespace Loom.Analyzer.Analyzers;

/// <summary>
/// Validates array indexing.
/// </summary>
[LoomAnalyzer]
public class ArrayBoundsAnalyzer : Analyzer
{
    public static Diagnostic NonArrayIndex = new(Diagnostic.DiagnosticLevel.Error, "Cannot index '{0}' because it is not an array.");
    public static Diagnostic InvalidIndexType = new(Diagnostic.DiagnosticLevel.Error, "Array indexes must be i32 values.");
    public static Diagnostic IndexOutOfBounds = new(Diagnostic.DiagnosticLevel.Error, "Array index {0} is outside the bounds of array '{1}' with size {2}.");

    [Visitor]
    public void Visit(ArrayAccessExpressionNode node)
    {
        var array = node.Receiver is IdentifierNameNode identifier ? Context.GetSymbol(identifier).Symbol : null;
        int? size = array switch
        {
            LocalVariableSymbol { Type: ArrayTypeSymbol type } => type.Size,
            FieldSymbol { Type: ArrayTypeSymbol type } => type.Size,
            _ => null
        };

        if (size == null)
        {
            Context.DiagnosticContext?.Report(NonArrayIndex, node.Receiver.StartToken?.Location, node.Receiver);
            return;
        }

        if (!Context.ExpressionTypes.TryGetValue(node.Index, out var indexType) || indexType.KnownType != TypeSymbol.DefaultType.I32)
        {
            Context.DiagnosticContext?.Report(InvalidIndexType, node.Index.StartToken?.Location);
            return;
        }

        if (node.Index is NumberLiteralNode literal && int.TryParse(literal.Value, out var index) && index >= size)
            Context.DiagnosticContext?.Report(IndexOutOfBounds, literal.StartToken?.Location, index, array!.Name, size);
    }
}
