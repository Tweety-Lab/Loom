using Loom.Parser.Tokenizer;
using static Loom.Parser.Tokenizer.Token;

namespace Loom.Parser.AST.Rules.Default;

public record InterfaceDeclarationNode(Token Name, TypeParameterListNode TypeParameters, InheritanceListNode Inheritance, BlockNode Body, List<Token> Modifiers, DocumentCommentListNode DocumentComments) : ASTNode, ITypeDeclarationNode, ITypeParameterizableNode, IInheritableNode, IDocumentableNode
{
    /// <inheritdoc/>
    public DocumentCommentListNode DocumentComments { get; set; } = DocumentComments;

    /// <inheritdoc/>
    public override IEnumerable<ASTNode> Children => [TypeParameters, Inheritance, Body];
}

[ParserRule]
public class InterfaceDeclarationRule : TypeMemberParserRule
{
    /// <inheritdoc/>
    public InterfaceDeclarationRule(LoomParser parser) : base(parser) { }

    /// <inheritdoc/>
    public override InterfaceDeclarationNode ParseNode()
    {
        var documentComments = RunRule<DocumentCommentListRule, DocumentCommentListNode>();

        var modifiers = Parser.Reader.ExpectMany(t => TokenRegistry.IsMemberModifier(t.Type));

        Parser.Reader.Expect(TokenType.Interface); // interface
        var name = Parser.Reader.Expect(TokenType.Identifier); // name

        var typeParameters = Parser.Reader.Check(TokenType.Less) ? RunRule<TypeParameterListRule, TypeParameterListNode>() : Parser.Empty(new TypeParameterListNode([]));

        var inheritance = Parser.Reader.Check(TokenType.Colon) ? RunRule<InheritanceListRule, InheritanceListNode>() : Parser.Empty(new InheritanceListNode([]));

        var body = RunRule<TypeDeclarationBlockRule, BlockNode>();

        return new InterfaceDeclarationNode(name, typeParameters, inheritance, body, modifiers, documentComments);
    }
}
