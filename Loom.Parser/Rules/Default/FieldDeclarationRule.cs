using Loom.Parser.AST;
using static Loom.Parser.Tokenizer.Token;

namespace Loom.Parser.Rules.Default;

public record FieldDeclarationNode(VariableDeclarationNode Variable) : ASTNode
{
    public override IEnumerable<ASTNode> Children => [Variable];
}

[ParserRule]
public class FieldDeclarationRule : ParserRule<FieldDeclarationNode>
{
    /// <inheritdoc/>
    public FieldDeclarationRule(LoomParser parser) : base(parser) { }

    /// <inheritdoc/>
    public override FieldDeclarationNode ParseNode()
    {
        var variable = RunRule<VariableDeclarationRule, VariableDeclarationNode>();
        Parser.Reader.Expect(TokenType.Semicolon); // ;
        return new FieldDeclarationNode(variable);
    }
}