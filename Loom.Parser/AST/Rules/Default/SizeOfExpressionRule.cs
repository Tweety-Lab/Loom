using static Loom.Parser.Tokenizer.Token;

namespace Loom.Parser.AST.Rules.Default;

public record SizeOfExpressionNode(TypeNode Type) : ExpressionNode
{
    /// <inheritdoc/>
    public override IEnumerable<ASTNode> Children => [Type];
}

[ParserRule]
public class SizeOfExpressionRule : ParserRule<SizeOfExpressionNode>
{
    /// <inheritdoc/>
    public SizeOfExpressionRule(LoomParser parser) : base(parser) { }

    /// <inheritdoc/>
    public override SizeOfExpressionNode ParseNode()
    {
        Parser.Reader.Expect(TokenType.SizeOf); // sizeof

        Parser.Reader.Expect(TokenType.LParen); // (
        TypeNode type = RunRule<TypeRule, TypeNode>(); // type
        Parser.Reader.Expect(TokenType.RParen); // )

        return new SizeOfExpressionNode(type);
    }
}
