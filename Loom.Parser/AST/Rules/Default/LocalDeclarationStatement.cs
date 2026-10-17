namespace Loom.Parser.AST.Rules.Default;

public record LocalDeclarationStatementNode(VariableDeclarationNode Variable) : StatementNode
{
    /// <inheritdoc/>
    public override IEnumerable<ASTNode> Children => [Variable];
}

[ParserRule]
public class LocalDeclarationStatementRule : StatementParserRule
{
    /// <inheritdoc/>
    public LocalDeclarationStatementRule(LoomParser parser) : base(parser) { }

    /// <inheritdoc/>
    public override LocalDeclarationStatementNode ParseNode() => new LocalDeclarationStatementNode(RunRule<VariableDeclarationRule, VariableDeclarationNode>());
}
