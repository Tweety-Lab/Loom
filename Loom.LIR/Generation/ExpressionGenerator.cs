using Loom.Analyzer;
using Loom.Analyzer.Symbols;
using Loom.Common;
using Loom.LIR.Objects;
using Loom.Parser.AST;
using Loom.Parser.Rules.Default;

namespace Loom.LIR.Generation;

/// <summary>
/// Handles conversion of Abstract Syntax Tree (AST) expressions into Loom Intermediate Representation (LIR).
/// </summary>
internal class ExpressionGenerator
{
    private CompilationContext context;
    private LIRCompilationUnit unit;
    private LIRFunction function;
    private Dictionary<string, LIRTempValue> locals;

    private LIRGenerator Generator => function.LIRGenerator;

    /// <summary> Initializes a new instance of the <see cref="ExpressionGenerator"/> class. </summary>
    public ExpressionGenerator(CompilationContext context, LIRCompilationUnit unit, LIRFunction function, Dictionary<string, LIRTempValue> locals)
    {
        this.context = context;
        this.unit = unit;
        this.function = function;
        this.locals = locals;
    }

    public LIRValue Emit(ExpressionNode node) => node switch
    {
        NumberLiteralNode num => new LIRConstantIntValue(int.Parse(num.Value)),
        IdentifierNameNode ident => Generator.EmitLoad(locals[ident.BaseName]),
        BinaryExpressionNode binary => EmitBinary(binary),
        CallExpressionNode call => EmitCall(call),
        _ => throw new Exception($"Unhandled expression: {node.GetType().Name}")
    };

    private LIRValue EmitBinary(BinaryExpressionNode node)
    {
        var left = Emit(node.Left);
        var right = Emit(node.Right);

        return node.Operator.Type switch
        {
            Parser.Tokenizer.Token.TokenType.Plus => Generator.EmitAdd(left, right),
            _ => throw new Exception($"Unhandled operator: {node.Operator.Text}")
        };
    }

    private LIRValue EmitCall(CallExpressionNode node)
    {
        var symbol = context.AnalysisContext.GetSymbol(node.MethodName).Symbol;
        if (symbol == null)
            throw new Exception($"Could not resolve call: {node.MethodName.Token.Text}");

        var target = unit.GetFunction(symbol.FullyQualifiedName);
        var args = node.Arguments.Select(Emit).ToArray();
        return Generator.EmitCall(target, args);
    }
}
