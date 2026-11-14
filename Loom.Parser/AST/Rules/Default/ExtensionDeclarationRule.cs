using Loom.Parser.Tokenizer;
using static Loom.Parser.Tokenizer.Token;

namespace Loom.Parser.AST.Rules.Default;

public record ExtensionDeclarationNode(Token TypeName, TypeParameterListNode TypeParameters, InheritanceListNode Inheritance, BlockNode Body, List<Token> Modifiers) : ASTNode, ITypeParameterizableNode, IInheritableNode
{
    /// <inheritdoc/>
    public override IEnumerable<ASTNode> Children => [TypeParameters, Inheritance, Body];
}


[ParserRule]
public class ExtensionDeclarationRule : TypeMemberParserRule
{
    /// <inheritdoc/>
    public ExtensionDeclarationRule(LoomParser parser) : base(parser) { }

    /// <inheritdoc/>
    public override ExtensionDeclarationNode ParseNode()
    {
        var modifiers = Parser.Reader.ExpectMany(t => TokenRegistry.IsMemberModifier(t.Type));

        Parser.Reader.Expect(TokenType.Extension); // extension

        var typeName = Parser.Reader.ExpectAny(t => TypeRule.IsTypeName(t.Type)); // type
        var typeParameters = Parser.Reader.Check(TokenType.Less) ? RunRule<TypeParameterListRule, TypeParameterListNode>() : Parser.Empty(new TypeParameterListNode([]));
        var inheritance = Parser.Reader.Check(TokenType.Colon) ? RunRule<InheritanceListRule, InheritanceListNode>() : Parser.Empty(new InheritanceListNode([]));

        BlockNode body = RunRule<TypeDeclarationBlockRule, BlockNode>();

        return new ExtensionDeclarationNode(typeName, typeParameters, inheritance, body, modifiers);
    }
}
