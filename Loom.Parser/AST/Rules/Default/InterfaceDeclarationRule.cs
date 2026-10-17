using Loom.Parser.Tokenizer;
using static Loom.Parser.Tokenizer.Token;

namespace Loom.Parser.AST.Rules.Default;

public record InterfaceDeclarationNode(Token Name, TypeParameterListNode TypeParameters, BlockNode Body, List<Token> Modifiers) : ASTNode, ITypeDeclarationNode, ITypeParameterizableNode
{
    /// <inheritdoc/>
    public override IEnumerable<ASTNode> Children => [TypeParameters, Body];
}

[ParserRule]
public class InterfaceDeclarationRule : TypeMemberParserRule
{
    /// <inheritdoc/>
    public InterfaceDeclarationRule(LoomParser parser) : base(parser) { }

    /// <inheritdoc/>
    public override InterfaceDeclarationNode ParseNode()
    {
        var modifiers = Parser.Reader.ExpectMany(t => TokenRegistry.IsMemberModifier(t.Type));

        Parser.Reader.Expect(TokenType.Interface); // interface
        var name = Parser.Reader.Expect(TokenType.Identifier); // name

        var typeParameters = Parser.Reader.Check(TokenType.Less) ? RunRule<TypeParameterListRule, TypeParameterListNode>() : new TypeParameterListNode([]);

        var body = RunRule<TypeDeclarationBlockRule, BlockNode>();

        return new InterfaceDeclarationNode(name, typeParameters, body, modifiers);
    }
}
