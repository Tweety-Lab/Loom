using Loom.Parser.AST;
using static Loom.Parser.Tokenizer.Token;

namespace Loom.Parser.Rules.Default;

public record MemberAccessExpressionNode(ExpressionNode Receiver, IdentifierNameNode Name) : ExpressionNode
{
    /// <inheritdoc/>
    public override IEnumerable<ASTNode> Children => [Receiver, Name];
}

[ParserRule]
public class PostfixExpressionRule : ParserRule<ExpressionNode>
{
    /// <inheritdoc/>
    public PostfixExpressionRule(LoomParser parser) : base(parser) { }

    /// <inheritdoc/>
    public override ExpressionNode ParseNode()
    {
        var expression = RunRule<PrimaryExpressionRule, ExpressionNode>();

        while (true)
        {
            if (Parser.Reader.Match(TokenType.Period))
            {
                var name = new IdentifierNameNode(Parser.Reader.Expect(TokenType.Identifier));
                expression = new MemberAccessExpressionNode(expression, name);
            }
            else if (Parser.Reader.Check(TokenType.LParen))
            {
                expression = Parser.GetRule<CallExpressionRule>().Parse(expression);
            }
            else
            {
                break;
            }
        }

        return expression;
    }
}