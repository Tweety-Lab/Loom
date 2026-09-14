using Loom.Parser.AST;
using static Loom.Parser.Tokenizer.Token;

namespace Loom.Parser.Rules.Default;

public record AssignmentStatementNode(ExpressionNode Target, ExpressionNode Value) : StatementNode
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
    public override AssignmentStatementNode ParseNode() => Parse(RunRule<PostfixExpressionRule, ExpressionNode>());

    /// <summary> Parses an assignment to a pre-parsed <paramref name="target"/> expression. </summary>
    public AssignmentStatementNode Parse(ExpressionNode target)
    {
        Parser.Reader.Expect(TokenType.Equals); // =

        var value = RunRule<ExpressionRule, ExpressionNode>(); // value

        return new AssignmentStatementNode(target, value);
    }
}