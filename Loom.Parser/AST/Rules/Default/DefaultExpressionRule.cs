using Loom.Parser.AST;
using Loom.Parser.AST.Rules;
using Loom.Parser.Tokenizer;
using static Loom.Parser.Tokenizer.Token;

namespace Loom.Parser.AST.Rules.Default;

public record DefaultExpressionNode(TypeNode? Type = null) : ExpressionNode
{
    /// <inheritdoc/>
    public override IEnumerable<ASTNode> Children => Enumerable.Empty<ASTNode>();
}

[ParserRule]
public class DefaultExpressionRule : ParserRule<DefaultExpressionNode>
{
    /// <inheritdoc/>
    public DefaultExpressionRule(LoomParser parser) : base(parser) { }

    /// <inheritdoc/>
    public override DefaultExpressionNode ParseNode()
    {
        Parser.Reader.Expect(TokenType.Default); // default

        TypeNode? type = null;

        if (Parser.Reader.Match(TokenType.LParen)) // (
        {
            type = RunRule<TypeRule, TypeNode>(); // type
            Parser.Reader.Expect(TokenType.RParen); // )
        }

        return new DefaultExpressionNode(type);
    }
}
