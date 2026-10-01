using Loom.Parser.AST;
using Loom.Parser.AST.Rules;
using Loom.Parser.Tokenizer;
using static Loom.Parser.Tokenizer.Token;

namespace Loom.Parser.AST.Rules.Default;

public record IfStatementNode(ExpressionNode Expression, BlockNode Body) : StatementNode
{
    /// <inheritdoc/>
    public override IEnumerable<ASTNode> Children => [Expression, Body];
}

[ParserRule]
public class IfStatementRule : ParserRule<IfStatementNode>
{
    /// <inheritdoc/>
    public IfStatementRule(LoomParser parser) : base(parser) { }

    /// <inheritdoc/>
    public override IfStatementNode ParseNode()
    {
        Parser.Reader.Expect(TokenType.If); // if

        Parser.Reader.Expect(TokenType.LParen); // (
        var condition = RunRule<ExpressionRule, ExpressionNode>();
        Parser.Reader.Expect(TokenType.RParen); // )

        if (!Parser.Reader.Check(TokenType.LBrace))
            return new IfStatementNode(condition, new BlockNode([RunRule<StatementRule, StatementNode>()])); // Hacky single line conditionals

        return new IfStatementNode(condition, RunRule<MethodBlockRule, BlockNode>());
    }
}
