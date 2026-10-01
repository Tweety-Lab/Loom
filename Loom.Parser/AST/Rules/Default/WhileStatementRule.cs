using Loom.Parser.AST;
using Loom.Parser.AST.Rules;
using Loom.Parser.Tokenizer;
using static Loom.Parser.Tokenizer.Token;

namespace Loom.Parser.AST.Rules.Default;

public record WhileStatementNode(ExpressionNode Expression, BlockNode Body) : StatementNode
{
    /// <inheritdoc/>
    public override IEnumerable<ASTNode> Children => [Expression, Body];
}

[ParserRule]
public class WhileStatementRule : ParserRule<WhileStatementNode>
{
    /// <inheritdoc/>
    public WhileStatementRule(LoomParser parser) : base(parser) { }

    /// <inheritdoc/>
    public override WhileStatementNode ParseNode()
    {
        Parser.Reader.Expect(TokenType.While); // while

        Parser.Reader.Expect(TokenType.LParen); // (
        var condition = RunRule<ExpressionRule, ExpressionNode>();
        Parser.Reader.Expect(TokenType.RParen); // )

        if (!Parser.Reader.Check(TokenType.LBrace))
            return new WhileStatementNode(condition, new BlockNode([RunRule<StatementRule, StatementNode>()])); // Hacky single line whiles

        return new WhileStatementNode(condition, RunRule<MethodBlockRule, BlockNode>());
    }
}
