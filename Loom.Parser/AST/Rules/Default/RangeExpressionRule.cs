using static Loom.Parser.Tokenizer.Token;

namespace Loom.Parser.AST.Rules.Default;

public record RangeExpressionNode(ExpressionNode Start, ExpressionNode End) : ExpressionNode
{
    /// <inheritdoc/>
    public override IEnumerable<ASTNode> Children => [Start, End];
}

[ParserRule]
public class RangeExpressionRule : ParserRule<RangeExpressionNode>
{
    /// <inheritdoc/>
    public RangeExpressionRule(LoomParser parser) : base(parser) { }

    /// <inheritdoc/>
    public override RangeExpressionNode ParseNode()
    {
        // We cant make these expressions for now because this itself is an expression and thus causes infinite recursion

        var start = RunRule<NumberLiteralExpressionRule, NumberLiteralNode>();

        Parser.Reader.Expect(TokenType.PeriodPeriod); // ..

        var end = RunRule<NumberLiteralExpressionRule, NumberLiteralNode>();

        return new RangeExpressionNode(start, end);
    }
}

