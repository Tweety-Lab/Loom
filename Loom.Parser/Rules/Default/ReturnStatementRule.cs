using Loom.Common.Exceptions;
using Loom.Parser.AST;

using static Loom.Parser.Tokenizer.Token;

namespace Loom.Parser.Rules.Default;

public record ReturnStatementNode(ExpressionNode? Expression = null) : StatementNode
{
    /// <inheritdoc/>
    public override IEnumerable<ASTNode> Children => Expression is not null ? new[] { Expression } : Enumerable.Empty<ASTNode>();
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

        if (Parser.Reader.Check(TokenType.Semicolon))
            return new ReturnStatementNode();

        return new ReturnStatementNode(RunRule<ExpressionRule, ExpressionNode>());
    }
}
