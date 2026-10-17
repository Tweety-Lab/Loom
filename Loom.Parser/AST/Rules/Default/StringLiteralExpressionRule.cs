using Loom.Parser.Literals;
using static Loom.Parser.Tokenizer.Token;

namespace Loom.Parser.AST.Rules.Default;

public record StringLiteralNode(int[] Value) : ExpressionNode
{
    /// <inheritdoc/>
    public override IEnumerable<ASTNode> Children => Enumerable.Empty<ASTNode>();
}

[ParserRule]
public class StringLiteralExpressionRule : ParserRule<StringLiteralNode>
{
    private readonly StringLiteralParser literalParser = new StringLiteralParser();

    /// <inheritdoc/>
    public StringLiteralExpressionRule(LoomParser parser) : base(parser) { }

    /// <inheritdoc/>
    public override StringLiteralNode ParseNode()
    {
        var token = Parser.Reader.Expect(TokenType.StringLiteral);

        if (literalParser.TryParse(token, out var value, out var diagnostic))
            return new StringLiteralNode(value);

        if (diagnostic != null)
            Parser.DiagnosticContext?.Report(diagnostic.Value);

        return new StringLiteralNode([]);
    }
}