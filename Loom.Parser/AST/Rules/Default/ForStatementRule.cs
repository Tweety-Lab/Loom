using Loom.Parser.AST;
using Loom.Parser.AST.Rules;
using Loom.Parser.Tokenizer;
using static Loom.Parser.Tokenizer.Token;

namespace Loom.Parser.AST.Rules.Default;

public record ForStatementNode(StatementNode Initializer, ExpressionNode Condition, StatementNode Incrementor, BlockNode Body) : StatementNode
{
    /// <inheritdoc/>
    public override IEnumerable<ASTNode> Children => [Initializer, Condition, Incrementor, Body];
}

[ParserRule]
public class ForStatementRule : ParserRule<ForStatementNode>
{
    /// <inheritdoc/>
    public ForStatementRule(LoomParser parser) : base(parser) { }

    /// <inheritdoc/>
    public override ForStatementNode ParseNode()
    {
        Parser.Reader.Expect(TokenType.For); // for

        Parser.Reader.Expect(TokenType.LParen); // (
        var initializer = Parser.GetRule<StatementRule>().ParseClause(); // initializer
        Parser.Reader.Expect(TokenType.Semicolon); // ;
        var condition = RunRule<ExpressionRule, ExpressionNode>(); // condition
        Parser.Reader.Expect(TokenType.Semicolon); // ;
        var incrementor = Parser.GetRule<StatementRule>().ParseClause(); // incrementor
        Parser.Reader.Expect(TokenType.RParen); // )

        if (!Parser.Reader.Check(TokenType.LBrace))
            return new ForStatementNode(initializer, condition, incrementor, new BlockNode([RunRule<StatementRule, StatementNode>()])); // Hacky single line fors

        return new ForStatementNode(initializer, condition, incrementor, RunRule<MethodBlockRule, BlockNode>());
    }
}