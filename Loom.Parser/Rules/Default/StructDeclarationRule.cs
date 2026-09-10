using Loom.Parser.AST;
using Loom.Parser.Tokenizer;
using static Loom.Parser.Tokenizer.Token;

namespace Loom.Parser.Rules.Default;

public record StructDeclarationNode(Token StructName, BlockNode Body, List<Token> Modifiers) : ASTNode
{
    /// <inheritdoc/>
    public override IEnumerable<ASTNode> Children => [Body];
}


[ParserRule]
public class StructDeclarationRule : ParserRule<StructDeclarationNode>
{
    /// <inheritdoc/>
    public StructDeclarationRule(LoomParser parser) : base(parser) { }

    /// <inheritdoc/>
    public override StructDeclarationNode ParseNode()
    {
        var modifiers = Parser.Reader.ExpectMany(t => TokenRegistry.IsModifier(t.Type));

        Parser.Reader.Expect(TokenType.Struct); // struct

        var structName = Parser.Reader.Expect(TokenType.Identifier); // name

        BlockNode body = Parser.GetRule<BlockRule>().ParseNode();

        return new StructDeclarationNode(structName, body, modifiers);
    }
}
