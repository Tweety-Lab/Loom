using static Loom.Parser.Tokenizer.Token;

namespace Loom.Parser.Rules.Default;

[ParserRule]
public class RelationalExpressionRule : ParserRule<ExpressionNode>
{
    /// <inheritdoc/>
    public RelationalExpressionRule(LoomParser parser) : base(parser) { }

    /// <inheritdoc/>
    public override ExpressionNode ParseNode()
    {
        var left = RunRule<AdditiveExpressionRule, ExpressionNode>();

        while (Parser.Reader.Current.Type is TokenType.Less or TokenType.Greater or TokenType.LessEqual or TokenType.GreaterEqual)
        {
            var op = Parser.Reader.Advance();
            var right = RunRule<AdditiveExpressionRule, ExpressionNode>();
            left = new BinaryExpressionNode(left, op, right);
        }

        return left;
    }
}