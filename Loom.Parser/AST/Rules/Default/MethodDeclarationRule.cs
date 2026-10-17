using Loom.Parser.Tokenizer;
using static Loom.Parser.Tokenizer.Token;

namespace Loom.Parser.AST.Rules.Default;

public record MethodDeclarationNode(TypeNode ReturnType, Token MethodName, ParameterListNode Parameters, TypeParameterListNode TypeParameters, List<Token> Modifiers, BlockNode? Body) : ASTNode, IModifiableNode, ITypeParameterizableNode
{
    /// <inheritdoc/>
    public override IEnumerable<ASTNode> Children => Parameters.Concat(TypeParameters.Concat(Body is not null ? [Body] : Enumerable.Empty<ASTNode>()));
}


[ParserRule]
public class MethodDeclarationRule : TypeMemberParserRule
{
    /// <inheritdoc/>
    public MethodDeclarationRule(LoomParser parser) : base(parser) { }

    /// <inheritdoc/>
    public override MethodDeclarationNode ParseNode()
    {
        var modifiers = Parser.Reader.ExpectMany(t => TokenRegistry.IsMemberModifier(t.Type));

        var returnType = RunRule<TypeRule, TypeNode>(); // return type
        var methodName = Parser.Reader.Expect(TokenType.Identifier); // name

        // Type Parameters
        var typeParameters = Parser.Reader.Check(TokenType.Less) ? RunRule<TypeParameterListRule, TypeParameterListNode>(): new TypeParameterListNode([]); // TODO: this should be nullable probably

        var parameters = RunRule<ParameterListRule, ParameterListNode>();

        BlockNode? body;
        if (Parser.Reader.Match(TokenType.Semicolon))
            body = null;
        else
            body = RunRule<MethodBlockRule, BlockNode>();

        return new MethodDeclarationNode(returnType, methodName, parameters, typeParameters, modifiers, body);
    }
}
