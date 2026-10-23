using Loom.Parser.Tokenizer;
using static Loom.Parser.Tokenizer.Token;

namespace Loom.Parser.AST.Rules.Default;

public record StructDeclarationNode(Token Name, TypeParameterListNode TypeParameters, InheritanceListNode Inheritance, BlockNode Body, List<Token> Modifiers) : ASTNode, ITypeDeclarationNode, ITypeParameterizableNode, IInheritableNode
{
    /// <inheritdoc/>
    public override IEnumerable<ASTNode> Children => [TypeParameters, Inheritance, Body];
}


[ParserRule]
public class StructDeclarationRule : TypeMemberParserRule
{
    /// <inheritdoc/>
    public StructDeclarationRule(LoomParser parser) : base(parser) { }

    /// <inheritdoc/>
    public override StructDeclarationNode ParseNode()
    {
        var modifiers = Parser.Reader.ExpectMany(t => TokenRegistry.IsMemberModifier(t.Type));

        Parser.Reader.Expect(TokenType.Struct); // struct

        var structName = Parser.Reader.Expect(TokenType.Identifier); // name

        var typeParameters = Parser.Reader.Check(TokenType.Less) ? RunRule<TypeParameterListRule, TypeParameterListNode>() : Parser.Empty(new TypeParameterListNode([]));

        var inheritance = Parser.Reader.Check(TokenType.Colon) ? RunRule<InheritanceListRule, InheritanceListNode>() : Parser.Empty(new InheritanceListNode([]));

        BlockNode body = RunRule<TypeDeclarationBlockRule, BlockNode>();

        return new StructDeclarationNode(structName, typeParameters, inheritance, body, modifiers);
    }
}
