using Loom.Parser.AST;
using Loom.Parser.AST.Rules;
using static Loom.Parser.Tokenizer.Token;

namespace Loom.Parser.AST.Rules.Default;
public record ReturnStatementNode(ExpressionNode? Expression = null) : StatementNode
{
    /// <inheritdoc/>
    public override IEnumerable<ASTNode> Children => Expression is not null ? [Expression] : Enumerable.Empty<ASTNode>();
}

[ParserRule]
public class ReturnStatementRule : ParserRule<ReturnStatementNode>
{
    /// <inheritdoc/>
    public ReturnStatementRule(LoomParser parser) : base(parser) { }

    /// <inheritdoc/>
    public override ReturnStatementNode ParseNode()
    {
        Parser.Reader.Expect(TokenType.Return); // return

        if (Parser.Reader.Check(TokenType.Semicolon))
            return new ReturnStatementNode();

        return new ReturnStatementNode(RunRule<ExpressionRule, ExpressionNode>());
    }
}
