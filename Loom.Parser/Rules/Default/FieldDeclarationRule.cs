using Loom.Parser.AST;
using Loom.Parser.Tokenizer;
using static Loom.Parser.Tokenizer.Token;

namespace Loom.Parser.Rules.Default;

public record FieldDeclarationNode(List<Token> Modifiers, VariableDeclarationNode Variable) : ASTNode
{
    /// <inheritdoc/>
    public override IEnumerable<ASTNode> Children => [Variable];

    /// <summary> Returns true if the field has the specified modifier. </summary>
    public bool HasModifier(TokenType type) => Modifiers.Any(m => m.Type == type);
}

[ParserRule]
public class FieldDeclarationRule : ParserRule<FieldDeclarationNode>
{
    /// <inheritdoc/>
    public FieldDeclarationRule(LoomParser parser) : base(parser) { }

    /// <inheritdoc/>
    public override FieldDeclarationNode ParseNode()
    {
        var modifiers = Parser.Reader.ExpectMany(t => TokenRegistry.IsMemberModifier(t.Type));

        var variable = RunRule<VariableDeclarationRule, VariableDeclarationNode>();
        Parser.Reader.Expect(TokenType.Semicolon); // ;
        return new FieldDeclarationNode(modifiers, variable);
    }
}