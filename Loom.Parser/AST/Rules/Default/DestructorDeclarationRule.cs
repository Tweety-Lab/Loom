using Loom.Parser.Tokenizer;
using static Loom.Parser.Tokenizer.Token;

namespace Loom.Parser.AST.Rules.Default;

public record DestructorDeclarationNode(Token Name, ParameterListNode Parameters, BlockNode Body, List<Token> Modifiers) : ASTNode, IModifiableNode
{
    /// <inheritdoc/>
    public override IEnumerable<ASTNode> Children => Parameters.Cast<ASTNode>().Prepend(Body);
}

[ParserRule]
public class DestructorDeclarationRule : TypeMemberParserRule
{
    /// <inheritdoc/>
    public DestructorDeclarationRule(LoomParser parser) : base(parser) { }

    /// <inheritdoc/>
    public override DestructorDeclarationNode ParseNode()
    {
        var modifiers = Parser.Reader.ExpectMany(t => TokenRegistry.IsMemberModifier(t.Type));
        Parser.Reader.Expect(TokenType.Tilde); // ~
        var methodName = Parser.Reader.Expect(TokenType.Identifier); // name

        var parameters = RunRule<ParameterListRule, ParameterListNode>();

        BlockNode body = body = RunRule<MethodBlockRule, BlockNode>();

        return new DestructorDeclarationNode(methodName, parameters, body, modifiers);
    }
}
