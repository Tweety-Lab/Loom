
using static Loom.Parser.Tokenizer.Token;

namespace Loom.Parser.Rules.Default;

[ParserRule]
public class MultiplicativeExpressionRule : ParserRule<ExpressionNode>
{
    /// <inheritdoc/>
    public MultiplicativeExpressionRule(LoomParser parser) : base(parser) { }

    /// <inheritdoc/>
    public override ExpressionNode ParseNode()
    {
        var left = RunRule<PrimaryExpressionRule, ExpressionNode>();

        while (Parser.Reader.Current.Type is TokenType.Star or TokenType.Slash)
        {
            var op = Parser.Reader.Advance();
            var right = RunRule<PrimaryExpressionRule, ExpressionNode>();
            left = new BinaryExpressionNode(left, op, right);
        }

        return left;
    }
}

