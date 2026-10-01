using Loom.Common.Diagnostics;
using Loom.Parser.AST;
using Loom.Parser.Literals;
using Loom.Parser.Tokenizer;
using static Loom.Parser.Tokenizer.Token;

namespace Loom.Parser.Rules.Default;

/// <summary> A boolean literal, holding the value it represents. </summary>
public record BooleanLiteralNode(bool Value) : ExpressionNode
{
    /// <inheritdoc/>
    public override IEnumerable<ASTNode> Children => Enumerable.Empty<ASTNode>();
}

[ParserRule]
public class BooleanLiteralExpressionRule : ParserRule<BooleanLiteralNode>
{
    private readonly BooleanLiteralParser literalParser = new BooleanLiteralParser();

    /// <inheritdoc/>
    public BooleanLiteralExpressionRule(LoomParser parser) : base(parser) { }

    /// <inheritdoc/>
    public override BooleanLiteralNode ParseNode()
    {
        var token = Parser.Reader.ExpectAny(t => t.Type is TokenType.True or TokenType.False);

        if (literalParser.TryParse(token, out var value, out var diagnostic))
            return new BooleanLiteralNode(value);

        if (diagnostic != null)
            Parser.DiagnosticContext?.Report(diagnostic.Value);

        return new BooleanLiteralNode(false);
    }
}
