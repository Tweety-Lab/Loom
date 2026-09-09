using static Loom.Parser.Tokenizer.Token;

namespace Loom.Parser.Rules.Default;

[ParserRule]
public class EqualityExpressionRule : ParserRule<ExpressionNode>
{
    /// <inheritdoc/>
    public EqualityExpressionRule(LoomParser parser) : base(parser) { }

    /// <inheritdoc/>
    public override ExpressionNode ParseNode()
    {
        var left = RunRule<RelationalExpressionRule, ExpressionNode>();

        while (Parser.Reader.Current.Type is TokenType.EqualEqual or TokenType.NotEqual)
        {
            var op = Parser.Reader.Advance();
            var right = RunRule<RelationalExpressionRule, ExpressionNode>();
            left = new BinaryExpressionNode(left, op, right);
        }

        return left;
    }
}