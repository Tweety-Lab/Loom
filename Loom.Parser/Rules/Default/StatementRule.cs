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

        bool isBuiltInType = TokenRegistry.IsBuiltInType(current);

        var statement = (StatementNode)(current switch
        {
            TokenType.Return => RunRule<ReturnStatementRule, ReturnStatementNode>(),

            _ when isBuiltInType && next == TokenType.Identifier=> RunRule<VariableDeclarationRule, VariableDeclarationNode>(),

            TokenType.Identifier when next == TokenType.Equals => RunRule<AssignmentStatementRule, AssignmentStatementNode>(),

            _ => new ExpressionStatementNode(RunRule<ExpressionRule, ExpressionNode>())
        });

        Parser.Reader.Expect(TokenType.Semicolon); // ;
        return statement;
    }
}
