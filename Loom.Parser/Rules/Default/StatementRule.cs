using Loom.Parser.AST;
using Loom.Parser.Tokenizer;

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
        var current = Parser.Reader.Current.Type;
        var next = Parser.Reader.Peek().Type;

        bool isType = TokenRegistry.IsBuiltInType(current) || current == TokenType.Identifier;

        var statement = (StatementNode)(current switch
        {
            TokenType.Return => RunRule<ReturnStatementRule, ReturnStatementNode>(),
            TokenType.If => RunRule<ConditionalRule, ConditionalNode>(),
            _ when isType && next == TokenType.Identifier => RunRule<LocalDeclarationStatementRule, LocalDeclarationStatementNode>(),
            _ => ParseExpressionOrAssignment()
        });

        if (current != TokenType.If)
            Parser.Reader.Expect(TokenType.Semicolon); // ;
        return statement;
    }

    private StatementNode ParseExpressionOrAssignment()
    {
        var expression = RunRule<ExpressionRule, ExpressionNode>();

        if (Parser.Reader.Current.Type != TokenType.Equals)
            return new ExpressionStatementNode(expression);

        return Parser.GetRule<AssignmentStatementRule>().Parse(expression);
    }
}
