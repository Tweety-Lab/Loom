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
        var callRule = Parser.GetRule<CallExpressionRule>();

        while (true)
        {
            callRule.Callee = expression;

            if (Parser.Reader.Match(TokenType.Period))
            {
                var name = new IdentifierNameNode(Parser.Reader.Expect(TokenType.Identifier));
                expression = new MemberAccessExpressionNode(expression, name);
            }
            else if (TryRunRule<CallExpressionRule, CallExpressionNode>(out var call))
            {
                expression = call;
            }
            else if (Parser.Reader.Match(TokenType.LBracket))
            {
                var index = RunRule<ExpressionRule, ExpressionNode>();
                Parser.Reader.Expect(TokenType.RBracket);
                expression = new ArrayAccessExpressionNode(expression, index);
            }
            else if (Parser.Reader.Current.Type is TokenType.PlusPlus or TokenType.MinusMinus)
            {
                var op = Parser.Reader.Advance();
                expression = new IncrementExpressionNode(expression, op, false);
            }
            else
            {
                break;
            }
        }

        return expression;
    }
}
