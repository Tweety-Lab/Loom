using Loom.Common.Exceptions;
using Loom.Parser.AST;

using static Loom.Parser.Tokenizer.Token;

namespace Loom.Parser.Rules.Default;

public record ReturnStatementNode() : StatementNode
{
    /// <inheritdoc/>
    public override IEnumerable<ASTNode> Children => Enumerable.Empty<ASTNode>();
}

[ParserRule]
public class ReturnStatementRule : ParserRule<ReturnStatementNode>
{
    /// <inheritdoc/>
    public ReturnStatementRule(LoomParser parser) : base(parser) { }

    /// <inheritdoc/>
    public override ReturnStatementNode Parse()
    {
        Parser.Reader.Expect(TokenType.Return); // return

        return new ReturnStatementNode();
    }
}
