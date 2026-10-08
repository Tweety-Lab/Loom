using Loom.Parser.AST;
using Loom.Parser.AST.Rules;
using static Loom.Parser.Tokenizer.Token;

namespace Loom.Parser.AST.Rules.Default;

public record UnsafeStatementNode(BlockNode Body) : StatementNode
{
    /// <inheritdoc/>
    public override IEnumerable<ASTNode> Children => [Body];
}

[ParserRule]
public class UnsafeStatementRule : ParserRule<UnsafeStatementNode>
{
    /// <inheritdoc/>
    public UnsafeStatementRule(LoomParser parser) : base(parser) { }

    /// <inheritdoc/>
    public override UnsafeStatementNode ParseNode()
    {
        Parser.Reader.Expect(TokenType.Unsafe); // unsafe

        return new UnsafeStatementNode(RunRule<MethodBlockRule, BlockNode>());
    }
}
