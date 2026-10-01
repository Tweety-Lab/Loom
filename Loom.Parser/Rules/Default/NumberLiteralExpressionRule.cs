using Loom.Common.Diagnostics;
using Loom.Parser.AST;
using Loom.Parser.Literals;
using Loom.Parser.Tokenizer;
using static Loom.Parser.Tokenizer.Token;

namespace Loom.Parser.Rules.Default;

/// <summary> An integer literal. </summary>
public record NumberLiteralNode(int Value) : ExpressionNode
{
    /// <inheritdoc/>
    public override IEnumerable<ASTNode> Children => Enumerable.Empty<ASTNode>();
}

[ParserRule]
public class NumberLiteralExpressionRule : ParserRule<NumberLiteralNode>
{
    private readonly NumberLiteralParser literalParser = new NumberLiteralParser();

    /// <inheritdoc/>
    public NumberLiteralExpressionRule(LoomParser parser) : base(parser) { }

    /// <inheritdoc/>
    public override NumberLiteralNode ParseNode()
    {
        var token = Parser.Reader.Expect(TokenType.Number);

        if (literalParser.TryParse(token, out var value, out var diagnostic))
            return new NumberLiteralNode(value);

        if (diagnostic != null)
            Parser.DiagnosticContext?.Report(diagnostic.Value);

        return new NumberLiteralNode(0);
    }
}
