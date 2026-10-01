using Loom.Parser.AST;
using Loom.Parser.AST.Rules;

namespace Loom.Parser.AST.Rules.Default;

public record LocalDeclarationStatementNode(VariableDeclarationNode Variable) : StatementNode
{
    /// <inheritdoc/>
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