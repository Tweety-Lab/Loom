using Loom.Parser.AST;
using static Loom.Parser.Tokenizer.Token;

namespace Loom.Parser.Rules.Default;

public record CharacterLiteralNode(string Value) : ExpressionNode
{
    /// <inheritdoc/>
    public override IEnumerable<ASTNode> Children => Enumerable.Empty<ASTNode>();
}

[ParserRule]
public class CharacterLiteralExpressionRule : ParserRule<CharacterLiteralNode>
{
    /// <inheritdoc/>
    public CharacterLiteralExpressionRule(LoomParser parser) : base(parser) { }

    /// <inheritdoc/>
    public override CharacterLiteralNode ParseNode()
    {
        Parser.Reader.Expect(TokenType.SingleQuote); // '
        var content = Parser.Reader.Advance().Text; // TODO: Parse multiple tokens (i.e., backslash + text)
        Parser.Reader.Expect(TokenType.SingleQuote); // '

        return new CharacterLiteralNode(content);
    }
}


