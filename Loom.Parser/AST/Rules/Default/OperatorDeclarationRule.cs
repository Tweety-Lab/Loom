using Loom.Parser.Tokenizer;
using static Loom.Parser.Tokenizer.Token;

namespace Loom.Parser.AST.Rules.Default;

public record OperatorDeclarationNode(TypeNode ReturnType, Token Operator, ParameterListNode Parameters, List<Token> Modifiers, BlockNode? Body, DocumentCommentListNode DocumentComments) : ASTNode, IModifiableNode, IDocumentableNode
{
    /// <inheritdoc/>
    public DocumentCommentListNode DocumentComments { get; set; } = DocumentComments;

    /// <inheritdoc/>
    public override IEnumerable<ASTNode> Children => new ASTNode[] { ReturnType, Parameters }.Concat(Body is not null ? [Body] : Enumerable.Empty<ASTNode>());
}

[ParserRule]
public class OperatorDeclarationRule : TypeMemberParserRule
{
    /// <inheritdoc/>
    public OperatorDeclarationRule(LoomParser parser) : base(parser) { }

    /// <inheritdoc/>
    public override OperatorDeclarationNode ParseNode()
    {
        var documentComments = RunRule<DocumentCommentListRule, DocumentCommentListNode>();

        var modifiers = Parser.Reader.ExpectMany(t => TokenRegistry.IsMemberModifier(t.Type));
        var returnType = RunRule<TypeRule, TypeNode>();
        Parser.Reader.Expect(TokenType.Operator);

        var op = Parser.Reader.ExpectAny(t => TokenRegistry.IsOperator(t.Type));

        if (op.Type is TokenType.LBracket or TokenType.LParen)
        {
            var closing = Parser.Reader.Expect(op.Type == TokenType.LBracket ? TokenType.RBracket : TokenType.RParen);
            op = new Token(op.Type, op.Text + closing.Text, op.Location) { Start = op.Start, End = closing.End };
        }

        var parameters = RunRule<ParameterListRule, ParameterListNode>();
        var body = Parser.Reader.Match(TokenType.Semicolon) ? null : RunRule<MethodBlockRule, BlockNode>();

        return new OperatorDeclarationNode(returnType, op, parameters, modifiers, body, documentComments);
    }
}
