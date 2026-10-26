using Loom.Parser.Tokenizer;
using static Loom.Parser.Tokenizer.Token;

namespace Loom.Parser.AST.Rules.Default;

public record ConstructorDeclarationNode(Token Name, ParameterListNode Parameters, BlockNode Body, List<Token> Modifiers, DocumentCommentListNode DocumentComments) : ASTNode, IModifiableNode, IDocumentableNode
{
    /// <inheritdoc/>
    public DocumentCommentListNode DocumentComments { get; set; } = DocumentComments;

    /// <inheritdoc/>
    public override IEnumerable<ASTNode> Children => Parameters.Cast<ASTNode>().Prepend(Body);
}


[ParserRule]
public class ConstructorDeclarationRule : TypeMemberParserRule
{
    /// <inheritdoc/>
    public ConstructorDeclarationRule(LoomParser parser) : base(parser) { }

    /// <inheritdoc/>
    public override ConstructorDeclarationNode ParseNode()
    {
        var documentComments = RunRule<DocumentCommentListRule, DocumentCommentListNode>();

        var modifiers = Parser.Reader.ExpectMany(t => TokenRegistry.IsMemberModifier(t.Type));
        var methodName = Parser.Reader.Expect(TokenType.Identifier); // name

        var parameters = RunRule<ParameterListRule, ParameterListNode>();

        BlockNode body = body = RunRule<MethodBlockRule, BlockNode>();

        return new ConstructorDeclarationNode(methodName, parameters, body, modifiers, documentComments);
    }
}
