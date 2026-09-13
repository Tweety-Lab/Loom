using Loom.Parser.AST;

namespace Loom.Parser.Rules.Default;

public record LocalDeclarationStatementNode(VariableDeclarationNode Variable) : StatementNode
{
    public override IEnumerable<ASTNode> Children => [Variable];
}

[ParserRule]
public class LocalDeclarationStatementRule : ParserRule<LocalDeclarationStatementNode>
{
    /// <inheritdoc/>
    public LocalDeclarationStatementRule(LoomParser parser) : base(parser) { }

    /// <inheritdoc/>
    public override LocalDeclarationStatementNode ParseNode() => new LocalDeclarationStatementNode(RunRule<VariableDeclarationRule, VariableDeclarationNode>());
}