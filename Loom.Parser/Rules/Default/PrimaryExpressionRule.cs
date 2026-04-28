using Loom.Parser.AST;

using static Loom.Parser.Tokenizer.Token;

namespace Loom.Parser.Rules.Default;


public abstract record ExpressionNode() : ASTNode
{
    /// <inheritdoc/>
    public override IEnumerable<ASTNode> Children => Enumerable.Empty<ASTNode>();
}

public record NumberLiteralNode(string Value) : ExpressionNode
{
    /// <inheritdoc/>
    public override IEnumerable<ASTNode> Children => Enumerable.Empty<ASTNode>();
}

[ParserRule]
public class ExpressionRule : ParserRule<ExpressionNode>
{
    /// <inheritdoc/>
    public ExpressionRule(LoomParser parser) : base(parser) { }

    /// <inheritdoc/>
    public override ExpressionNode ParseNode() => RunRule<AdditiveExpressionRule, ExpressionNode>();
}

[ParserRule]
public class PrimaryExpressionRule : ParserRule<ExpressionNode>
{
    /// <inheritdoc/>
    public PrimaryExpressionRule(LoomParser parser) : base(parser) { }

    /// <inheritdoc/>
    public override ExpressionNode ParseNode()
    {
        return Parser.Reader.Current.Type switch
        {
            TokenType.Number => new NumberLiteralNode(Parser.Reader.Advance().Value),
            TokenType.Identifier when Parser.Reader.Peek().Type == TokenType.LParen => RunRule<CallExpressionRule, CallExpressionNode>(),
            _ => throw new Exception($"Unexpected token: '{Parser.Reader.Current.Value}' type={Parser.Reader.Current.Type} at {Parser.Reader.Current.Location}")
        };
    }
}

