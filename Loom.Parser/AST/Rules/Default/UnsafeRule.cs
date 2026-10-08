using Loom.Parser.AST;
using Loom.Parser.AST.Rules;
using static Loom.Parser.Tokenizer.Token;

namespace Loom.Parser.AST.Rules.Default;

public record UnsafeStatementNode(BlockNode Body) : ASTNode
{
    /// <inheritdoc/>
    public override IEnumerable<ASTNode> Children => [Body];
}

[ParserRule]
public class UnsafeRule : ParserRule<UnsafeStatementNode>
{
    /// <inheritdoc/>
    public UnsafeRule(LoomParser parser) : base(parser) { }

    /// <inheritdoc/>
    public override UnsafeStatementNode ParseNode()
    {
        Parser.Reader.Expect(TokenType.Unsafe); // unsafe

        return new UnsafeStatementNode(RunRule<MethodBlockRule, BlockNode>());
    }
}
