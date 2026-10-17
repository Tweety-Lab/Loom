using Loom.Analyzer;
using Loom.Analyzer.Symbols;
using Loom.Common;
using Loom.LIR.Objects;
using Loom.Parser.AST;
using Loom.Parser.AST.Rules.Default;

namespace Loom.LIR.Generation;

internal partial class ExpressionGenerator
{
    /// <summary> Emits an assignment to a property through its set accessor, or its backing field when it has none. </summary>
    public void EmitAssignment(AssignmentStatementNode node)
    {
        if (ResolveProperty(node.Target) is { } property)
        {
            EmitPropertySet(property, node.Target, node.Value);
            return;
        }

        var address = EmitStorage(node.Target);

        // Assigning to a whole array defaults each of its elements rather than storing a single value
        if (context.AnalysisContext.ExpressionTypes.TryGetValue(node.Target, out var targetType) && targetType is ArrayTypeSymbol)
        {
            Initialize(address, node.Value, targetType);
            return;
        }

        Generator.EmitStore(EmitValue(node.Value, context.AnalysisContext.ExpressionTypes[node.Target]), address);
    }

    public LIRValue EmitSizeOf(SizeOfExpressionNode node)
    {
        TypeSymbol? type = (TypeSymbol?)context.AnalysisContext.GetSymbol(node.Type).Symbol;

        if (type == null)
            throw new Exception($"Could not resolve size of type {node.Type}.");

        return Generator.EmitSizeOf(ASTGenerator.ConvertType(type, substitution));
    }

    private static bool IsWide(LIRType type) => type is LIRIntPtrType or LIRIntType { Bits: > 32 };

    private LIRValue EmitIncrement(IncrementExpressionNode node)
    {
        if (ResolveProperty(node.Target) is { } property)
        {
            var self = EmitPropertySelf(property, node.Target);
            var oldPropertyValue = EmitPropertyGet(property, node.Target, self);
            var newPropertyValue = ApplyDelta(oldPropertyValue, node.Operator);

            EmitPropertySet(property, node.Target, self, newPropertyValue);

            return node.IsPrefix ? newPropertyValue : oldPropertyValue;
        }

        var address = EmitStorage(node.Target);
        var oldValue = Generator.EmitLoad(address);
        var newValue = ApplyDelta(oldValue, node.Operator);

        Generator.EmitStore(newValue, address);

        return node.IsPrefix ? newValue : oldValue;
    }

    private LIRValue ApplyDelta(LIRValue value, Parser.Tokenizer.Token op)
    {
        var one = Generator.EmitConvert(new LIRConstantIntValue(1), value.Type);

        return op.Type == Parser.Tokenizer.Token.TokenType.PlusPlus
            ? Generator.EmitAdd(value, one)
            : Generator.EmitSub(value, one);
    }

    private LIRValue EmitBinary(BinaryExpressionNode node)
    {
        var left = Emit(node.Left);
        var right = Emit(node.Right);

        if (left.Type != right.Type && LIRGenerator.IsIntegerType(left.Type) && LIRGenerator.IsIntegerType(right.Type))
        {
            LIRType common = IsWide(left.Type) || !IsWide(right.Type) ? left.Type : right.Type;

            left = Generator.EmitConvert(left, common);
            right = Generator.EmitConvert(right, common);
        }

        return node.Operator.Type switch
        {
            Parser.Tokenizer.Token.TokenType.Plus => Generator.EmitAdd(left, right),
            Parser.Tokenizer.Token.TokenType.Minus => Generator.EmitSub(left, right),
            Parser.Tokenizer.Token.TokenType.Star => Generator.EmitMul(left, right),
            Parser.Tokenizer.Token.TokenType.Slash => Generator.EmitDiv(left, right),
            Parser.Tokenizer.Token.TokenType.EqualEqual => Generator.EmitCmpEq(left, right),
            Parser.Tokenizer.Token.TokenType.NotEqual => Generator.EmitCmpNe(left, right),
            Parser.Tokenizer.Token.TokenType.Less => Generator.EmitCmpLt(left, right),
            Parser.Tokenizer.Token.TokenType.Greater => Generator.EmitCmpGt(left, right),
            Parser.Tokenizer.Token.TokenType.LessEqual => Generator.EmitCmpLe(left, right),
            Parser.Tokenizer.Token.TokenType.GreaterEqual => Generator.EmitCmpGe(left, right),
            _ => throw new Exception($"Unhandled operator: {node.Operator.Text}")
        };
    }
}
