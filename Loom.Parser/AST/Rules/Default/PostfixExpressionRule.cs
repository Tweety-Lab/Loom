using static Loom.Parser.Tokenizer.Token;

namespace Loom.Parser.AST.Rules.Default;

public record MemberAccessExpressionNode(ExpressionNode Receiver, IdentifierNameNode Name) : ExpressionNode(Span.Cover(Receiver.Span, Name.Span))
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
        var startPosition = Parser.Reader.Position;
        var expression = RunRule<PrimaryExpressionRule, ExpressionNode>();
        var callRule = Parser.GetRule<CallExpressionRule>();

        while (true)
        {
            callRule.Callee = expression;

            if (Parser.Reader.Match(TokenType.Period))
            {
                var token = Parser.Reader.Expect(TokenType.Identifier);
                var name = new IdentifierNameNode(token);
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
                expression = Parser.Complete(new ArrayAccessExpressionNode(expression, index), startPosition);
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
