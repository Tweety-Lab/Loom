using Loom.Parser.AST;
using Loom.Parser.AST.Rules;
using Loom.Parser.Tokenizer;
using static Loom.Parser.Tokenizer.Token;

namespace Loom.Parser.AST.Rules.Default;

public record ParameterNode(TypeNode Type, Token Name) : ASTNode
{
    public override IEnumerable<ASTNode> Children => Enumerable.Empty<ASTNode>();
}

public record MethodDeclarationNode(TypeNode ReturnType, Token MethodName, List<ParameterNode> Parameters, List<Token> Modifiers, BlockNode? Body) : ASTNode, IModifiableNode
{
    /// <inheritdoc/>
    public override IEnumerable<ASTNode> Children => Body is not null ? [Body] : Enumerable.Empty<ASTNode>();
}


[ParserRule]
public class MethodDeclarationRule : ParserRule<MethodDeclarationNode>
{
    /// <inheritdoc/>
    public MethodDeclarationRule(LoomParser parser) : base(parser) { }

    /// <inheritdoc/>
    public override MethodDeclarationNode ParseNode()
    {
        var modifiers = Parser.Reader.ExpectMany(t => TokenRegistry.IsMemberModifier(t.Type));

        var returnType = RunRule<TypeRule, TypeNode>(); // return type
        var methodName = Parser.Reader.Expect(TokenType.Identifier); // name

        Parser.Reader.Expect(TokenType.LParen); // (

        // Paramaters
        var parameters = new List<ParameterNode>();
        if (Parser.Reader.Peek(0).Type != TokenType.RParen)
        {
            do
            {
                var type = RunRule<TypeRule, TypeNode>(); // type
                var name = Parser.Reader.Expect(TokenType.Identifier);

                parameters.Add(new ParameterNode(type, name));
            }
            while (Parser.Reader.Match(TokenType.Comma));
        }

        Parser.Reader.Expect(TokenType.RParen); // )

        BlockNode? body;
        if (Parser.Reader.Match(TokenType.Semicolon))
            body = null;
        else
            body = RunRule<MethodBlockRule, BlockNode>();

        return new MethodDeclarationNode(returnType, methodName, parameters, modifiers, body);
    }
}