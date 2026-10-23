using static Loom.Parser.Tokenizer.Token;

namespace Loom.Parser.AST.Rules.Default;

/// <summary>
/// Base class for rules that automatically get parsed as statements.
/// </summary>
public abstract class StatementParserRule : ParserRule<StatementNode>
{
    /// <summary> Whether the statement must be followed by a semicolon. </summary>
    public virtual bool RequiresTerminator => true;

    /// <inheritdoc/>
    protected StatementParserRule(LoomParser parser) : base(parser) { }
}

public abstract record StatementNode : ASTNode
{
    protected StatementNode() { }
    protected StatementNode(Span span) : base(span) { }
}

public record ExpressionStatementNode(ExpressionNode Expression) : StatementNode(Expression.Span)
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
        var statement = ParseStatement();

        if (statement.RequiresTerminator)
            Parser.Reader.Expect(TokenType.Semicolon); // ;

        return statement.Statement;
    }

    // TODO: Should we split this into BareStatement and Statement?
    /// <summary> Parses a statement whose <c>;</c> terminator is left for the caller to consume. </summary>
    public StatementNode ParseClause()
    {
        var startPosition = Parser.Reader.Position;
        var statement = ParseStatement();
        return Parser.Complete(statement.Statement, startPosition);
    }

    private (StatementNode Statement, bool RequiresTerminator) ParseStatement()
    {
        bool requiresTerminator = true;
        foreach (var rule in Parser.GetRules<StatementParserRule>())
            if (TryRunRule(rule, out var statement))
            {
                requiresTerminator = rule.RequiresTerminator;
                return ((StatementNode)statement, requiresTerminator);
            }

        return (ParseExpressionOrAssignment(), false);
    }

    private StatementNode ParseExpressionOrAssignment()
    {
        var expression = RunRule<ExpressionRule, ExpressionNode>();

        if (Parser.Reader.Current.Type != TokenType.Equals)
            return new ExpressionStatementNode(expression);

        return Parser.GetRule<AssignmentStatementRule>().Parse(expression);
    }
}
