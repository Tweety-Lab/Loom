using Loom.Parser.AST;
using Loom.Parser.AST.Rules;
using static Loom.Parser.Tokenizer.Token;

namespace Loom.Parser.AST.Rules.Default;

public record MemberAccessExpressionNode(ExpressionNode Receiver, IdentifierNameNode Name) : ExpressionNode
{
    /// <inheritdoc/>
    public override IEnumerable<ASTNode> Children => [Receiver, Name];
}

public record ArrayAccessExpressionNode(ExpressionNode Receiver, ExpressionNode Index) : ExpressionNode
{
    /// <inheritdoc/>
    public override IEnumerable<ASTNode> Children => [Receiver, Index];
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
            else if (CallExpressionRule.CanParse(Parser.Reader))
            {
                expression = Parser.GetRule<CallExpressionRule>().Parse(expression);
            }
            else if (Parser.Reader.Match(TokenType.LBracket))
            {
                var index = RunRule<ExpressionRule, ExpressionNode>();
                Parser.Reader.Expect(TokenType.RBracket);
                expression = new ArrayAccessExpressionNode(expression, index);
            }
            else
            {
                break;
            }
        }

        return expression;
    }
}
