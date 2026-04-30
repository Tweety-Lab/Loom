using Loom.Parser.AST;

using static Loom.Parser.Tokenizer.Token;

namespace Loom.Parser.Rules.Default;

public abstract record StatementNode() : ASTNode;

public record ExpressionStatementNode(ExpressionNode Expression) : StatementNode
{
    /// <inheritdoc/>
    public override IEnumerable<ASTNode> Children => [Expression];
}

[ParserRule]
public class StatementRule : ParserRule<StatementNode>
{
    /// <inheritdoc/>
    public StatementRule(LoomParser parser) : base(parser) { }

    /// <inheritdoc/>
    public override StatementNode ParseNode()
    {
        var statement = (StatementNode)(Parser.Reader.Current.Type switch
        {
            TokenType.Return => RunRule<ReturnStatementRule, ReturnStatementNode>(),
            (TokenType.I32 or TokenType.Identifier) when Parser.Reader.Peek().Type == TokenType.Identifier => RunRule<VariableDeclarationRule, VariableDeclarationNode>(),
            TokenType.Identifier when Parser.Reader.Peek().Type == TokenType.Equals => RunRule<AssignmentStatementRule, AssignmentStatementNode>(),
            _ => new ExpressionStatementNode(RunRule<ExpressionRule, ExpressionNode>())
        });

        Parser.Reader.Expect(TokenType.Semicolon); // ;
        return statement;
    }
}
