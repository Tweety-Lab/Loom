using Loom.Parser.AST;
using Loom.Parser.AST.Rules;
using static Loom.Parser.Tokenizer.Token;

namespace Loom.Parser.AST.Rules.Default;

public record UnsafeNode(BlockNode Body) : ASTNode
{
    /// <inheritdoc/>
    public override IEnumerable<ASTNode> Children => [Body];
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

        return new UnsafeNode(RunRule<MethodBlockRule, BlockNode>());
    }
}
