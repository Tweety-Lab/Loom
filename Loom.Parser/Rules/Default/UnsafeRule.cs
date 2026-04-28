using Loom.Parser.AST;

using static Loom.Parser.Tokenizer.Token;

namespace Loom.Parser.Rules.Default;

public record UnsafeNode(BlockNode Body) : ASTNode
{
    /// <inheritdoc/>
    public override IEnumerable<ASTNode> Children => new[] { Body };
}

[ParserRule]
public class UnsafeRule : ParserRule<UnsafeNode>
{
    /// <inheritdoc/>
    public UnsafeRule(LoomParser parser) : base(parser) { }

    /// <inheritdoc/>
    public override UnsafeNode ParseNode()
    {
        Parser.Reader.Expect(TokenType.Unsafe); // unsafe

        return new UnsafeNode(Parser.GetRule<MethodBlockRule>().ParseNode());
    }
}
