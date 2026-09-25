using Loom.Parser.AST;
using Loom.Parser.Tokenizer;
using static Loom.Parser.Tokenizer.Token;

namespace Loom.Parser.Rules.Default;

// TODO: Better place for this
public interface ITypeDeclarationNode
{
    Token Name { get; }
    BlockNode Body { get; }
    List<Token> Modifiers { get; }
}

public record StructDeclarationNode(Token Name, BlockNode Body, List<Token> Modifiers) : ASTNode, ITypeDeclarationNode
{
    /// <inheritdoc/>
    public override IEnumerable<ASTNode> Children => [Body];

    /// <summary> Returns true if the struct has the specified modifier. </summary>
    public bool HasModifier(TokenType type) => Modifiers.Any(m => m.Type == type);
}


[ParserRule]
public class StructDeclarationRule : ParserRule<StructDeclarationNode>
{
    /// <inheritdoc/>
    public StructDeclarationRule(LoomParser parser) : base(parser) { }

    /// <inheritdoc/>
    public override StructDeclarationNode ParseNode()
    {
        var modifiers = Parser.Reader.ExpectMany(t => TokenRegistry.IsMemberModifier(t.Type));

        Parser.Reader.Expect(TokenType.Struct); // struct

        var structName = Parser.Reader.Expect(TokenType.Identifier); // name

        BlockNode body = RunRule<TypeBlockRule, BlockNode>();

        return new StructDeclarationNode(structName, body, modifiers);
    }
}
