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
        var current = Parser.Reader.Current.Type;
        var next = Parser.Reader.Peek().Type;

        bool isType = TypeRule.IsTypeName(current);

        return current switch
        {
            TokenType.Return => RunRule<ReturnStatementRule, ReturnStatementNode>(),
            TokenType.If => RunRule<IfStatementRule, IfStatementNode>(),
            TokenType.While => RunRule<WhileStatementRule, WhileStatementNode>(),
            TokenType.For => RunRule<ForStatementRule, ForStatementNode>(),
            _ when isType && next == TokenType.Identifier => RunRule<LocalDeclarationStatementRule, LocalDeclarationStatementNode>(),
            _ when IsLocalDeclaration() => RunRule<LocalDeclarationStatementRule, LocalDeclarationStatementNode>(),
            _ => ParseExpressionOrAssignment()
        };
    }

    private StatementNode ParseExpressionOrAssignment()
    {
        var expression = RunRule<ExpressionRule, ExpressionNode>();

        if (Parser.Reader.Current.Type != TokenType.Equals)
            return new ExpressionStatementNode(expression);

        return Parser.GetRule<AssignmentStatementRule>().Parse(expression);
    }

    // Hacky
    private bool IsLocalDeclaration()
    {
        int offset = TypeRule.SkipModifiers(Parser.Reader, 0);

        if (!TypeRule.IsTypeName(Parser.Reader.Peek(offset).Type))
            return false;

        offset = TypeRule.SkipType(Parser.Reader, offset);
        return Parser.Reader.Peek(offset).Type == TokenType.Identifier;
    }
}
