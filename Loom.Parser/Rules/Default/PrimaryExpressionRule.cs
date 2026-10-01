using Loom.Parser.AST;
using Loom.Parser.Tokenizer;
using static Loom.Parser.Tokenizer.Token;

namespace Loom.Parser.Rules.Default;


public abstract record ExpressionNode() : ASTNode
{
    /// <inheritdoc/>
    public override IEnumerable<ASTNode> Children => Enumerable.Empty<ASTNode>();
}

// Storing the token is a bit hacky, we don't really need to
public record DefaultLiteralNode(Token Token) : ExpressionNode
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
    public override ExpressionNode ParseNode() => RunRule<EqualityExpressionRule, ExpressionNode>();
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
            TokenType.CharacterLiteral => RunRule<CharacterLiteralExpressionRule, CharacterLiteralNode>(),
            TokenType.True or TokenType.False => RunRule<BooleanLiteralExpressionRule, BooleanLiteralNode>(),
            TokenType.Number => RunRule<NumberLiteralExpressionRule, NumberLiteralNode>(),
            TokenType.New => RunRule<ObjectCreationExpressionRule, ObjectCreationExpressionNode>(),
            TokenType.Identifier => new IdentifierNameNode(Parser.Reader.Advance()),
            TokenType.Default => new DefaultLiteralNode(Parser.Reader.Advance()),
            _ => throw new Exception($"Unexpected token: '{Parser.Reader.Current.Text}' type={Parser.Reader.Current.Type} at {Parser.Reader.Current.Location}")
        };
    }
}

