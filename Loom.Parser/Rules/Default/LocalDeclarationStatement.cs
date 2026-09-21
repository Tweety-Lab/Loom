using Loom.Parser.AST;
using Loom.Parser.Tokenizer;
using static Loom.Parser.Tokenizer.Token;

namespace Loom.Parser.Rules.Default;

public record LocalDeclarationStatementNode(List<Token> Modifiers, VariableDeclarationNode Variable) : StatementNode
{
    public override IEnumerable<ASTNode> Children => [Variable];

    /// <summary> Returns true if the local has the specified modifier. </summary>
    public bool HasModifier(TokenType type) => Modifiers.Any(m => m.Type == type);
}

[ParserRule]
public class LocalDeclarationStatementRule : ParserRule<LocalDeclarationStatementNode>
{
    /// <inheritdoc/>
    public LocalDeclarationStatementRule(LoomParser parser) : base(parser) { }

    /// <inheritdoc/>
    public override LocalDeclarationStatementNode ParseNode()
    {
        var modifiers = Parser.Reader.ExpectMany(t => TokenRegistry.IsModifier(t.Type));

        var variable = RunRule<VariableDeclarationRule, VariableDeclarationNode>();
        return new LocalDeclarationStatementNode(modifiers, variable);
    }
}