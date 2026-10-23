using Loom.Parser.Tokenizer;
using static Loom.Parser.Tokenizer.Token;

namespace Loom.Parser.AST.Rules.Default;

public record IncrementExpressionNode(ExpressionNode Target, Token Operator, bool IsPrefix) : ExpressionNode(IsPrefix ? Span.Cover(Span.FromToken(Operator), Target.Span) : Span.Cover(Target.Span, Span.FromToken(Operator)))
{
    /// <inheritdoc/>
    public override IEnumerable<ASTNode> Children => [Target];
}

[ParserRule]
public class UnaryExpressionRule : ParserRule<ExpressionNode>
{
    /// <inheritdoc/>
    public UnaryExpressionRule(LoomParser parser) : base(parser) { }

    /// <inheritdoc/>
    public override ExpressionNode ParseNode()
    {
        if (Parser.Reader.Current.Type is TokenType.PlusPlus or TokenType.MinusMinus)
        {
            var op = Parser.Reader.Advance();
            return new IncrementExpressionNode(RunRule<UnaryExpressionRule, ExpressionNode>(), op, true);
        }

        return RunRule<PostfixExpressionRule, ExpressionNode>();
    }
}
