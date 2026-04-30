using Loom.Parser.AST;
using static Loom.Parser.Tokenizer.Token;

namespace Loom.Parser.Rules.Default;

public record AssignmentStatementNode(IdentifierNameNode Target, ExpressionNode Value) : StatementNode
{
    /// <inheritdoc/>
    public override IEnumerable<ASTNode> Children => [Target, Value];
}

[ParserRule]
public class AssignmentStatementRule : ParserRule<AssignmentStatementNode>
{
    /// <inheritdoc/>
    public AssignmentStatementRule(LoomParser parser) : base(parser) { }

    /// <inheritdoc/>
    public override AssignmentStatementNode ParseNode()
    {
        var target = Parser.Reader.Expect(TokenType.Identifier); // name

        Parser.Reader.Expect(TokenType.Equals); // =

        var value = RunRule<ExpressionRule, ExpressionNode>(); // value

        return new AssignmentStatementNode(new IdentifierNameNode(target), value);
    }
}