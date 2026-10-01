using Loom.Common.Diagnostics;
using Loom.Parser.AST;
using Loom.Parser.AST.Rules;
using Loom.Parser.Literals;
using Loom.Parser.Tokenizer;
using static Loom.Parser.Tokenizer.Token;

namespace Loom.Parser.AST.Rules.Default;

/// <summary> A character literal, holding the Unicode scalar value it represents. </summary>
public record CharacterLiteralNode(int Value) : ExpressionNode
{
    /// <inheritdoc/>
    public override IEnumerable<ASTNode> Children => Enumerable.Empty<ASTNode>();
}

[ParserRule]
public class CharacterLiteralExpressionRule : ParserRule<CharacterLiteralNode>
{
    private readonly CharacterLiteralParser literalParser = new CharacterLiteralParser();

    /// <inheritdoc/>
    public CharacterLiteralExpressionRule(LoomParser parser) : base(parser) { }

    /// <inheritdoc/>
    public override CharacterLiteralNode ParseNode()
    {
        var token = Parser.Reader.Expect(TokenType.CharacterLiteral);

        if (literalParser.TryParse(token, out var value, out var diagnostic))
            return new CharacterLiteralNode(value);

        if (diagnostic != null)
            Parser.DiagnosticContext?.Report(diagnostic.Value);

        return new CharacterLiteralNode(0);
    }
}
