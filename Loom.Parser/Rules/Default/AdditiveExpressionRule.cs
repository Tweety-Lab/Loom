
using Loom.Parser.AST;
using Loom.Parser.Tokenizer;
using static Loom.Parser.Tokenizer.Token;

namespace Loom.Parser.Rules.Default;

public record BinaryExpressionNode(ExpressionNode Left, Token Operator, ExpressionNode Right) : ExpressionNode
{
    /// <inheritdoc/>
    public override IEnumerable<ASTNode> Children => [Left, Right];
}

[ParserRule]
public class AdditiveExpressionRule : ParserRule<ExpressionNode>
{
    /// <inheritdoc/>
    public AdditiveExpressionRule(LoomParser parser) : base(parser) { }

    /// <inheritdoc/>
    public override ExpressionNode ParseNode()
    {
        var left = RunRule<MultiplicativeExpressionRule, ExpressionNode>();

        while (Parser.Reader.Current.Type is TokenType.Plus)
        {
            var op = Parser.Reader.Advance();
            var right = RunRule<MultiplicativeExpressionRule, ExpressionNode>();
            left = new BinaryExpressionNode(left, op, right);
        }

        return left;
    }
}
