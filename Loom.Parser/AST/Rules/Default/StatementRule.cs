using Loom.Parser.AST;
using Loom.Parser.AST.Rules;
using Loom.Parser.Tokenizer;

using static Loom.Parser.Tokenizer.Token;

namespace Loom.Parser.AST.Rules.Default;

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
        var current = Parser.Reader.Current.Type;
        var statement = ParseStatement();

        if (current is not (TokenType.If or TokenType.While or TokenType.For))
            Parser.Reader.Expect(TokenType.Semicolon); // ;

        return statement;
    }

    // TODO: Should we split this into BareStatement and Statement?
    /// <summary> Parses a statement whose <c>;</c> terminator is left for the caller to consume. </summary>
    public StatementNode ParseClause()
    {
        var startToken = Parser.Reader.Current;
        var statement = ParseStatement();
        statement.StartToken = startToken;
        return statement;
    }

    private StatementNode ParseStatement()
    {
        if (TryRunRule<ReturnStatementRule, ReturnStatementNode>(out var returnStatement))
            return returnStatement;

        if (TryRunRule<IfStatementRule, IfStatementNode>(out var ifStatement))
            return ifStatement;

        if (TryRunRule<WhileStatementRule, WhileStatementNode>(out var whileStatement))
            return whileStatement;

        if (TryRunRule<ForStatementRule, ForStatementNode>(out var forStatement))
            return forStatement;

        if (TryRunRule<LocalDeclarationStatementRule, LocalDeclarationStatementNode>(out var localDeclarationStatement))
            return localDeclarationStatement;

        return ParseExpressionOrAssignment();
    }

    private StatementNode ParseExpressionOrAssignment()
    {
        var expression = RunRule<ExpressionRule, ExpressionNode>();

        if (Parser.Reader.Current.Type != TokenType.Equals)
            return new ExpressionStatementNode(expression);

        return Parser.GetRule<AssignmentStatementRule>().Parse(expression);
    }
}
