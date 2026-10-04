using Loom.Parser.AST;
using Loom.Parser.AST.Rules;
using Loom.Parser.Tokenizer;
using static Loom.Parser.Tokenizer.Token;

namespace Loom.Parser.AST.Rules.Default;

public record ConstructorDeclarationNode(Token Name, List<ParameterNode> Parameters, BlockNode Body, List<Token> Modifiers) : ASTNode, IModifiableNode
{
    /// <inheritdoc/>
    public override IEnumerable<ASTNode> Children => Parameters.Cast<ASTNode>().Prepend(Body);
}


[ParserRule]
public class ConstructorDeclarationRule : ParserRule<ConstructorDeclarationNode>
{
    /// <inheritdoc/>
    public ConstructorDeclarationRule(LoomParser parser) : base(parser) { }

    /// <inheritdoc/>
    public override ConstructorDeclarationNode ParseNode()
    {
        var modifiers = Parser.Reader.ExpectMany(t => TokenRegistry.IsMemberModifier(t.Type));
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

        BlockNode body = body = RunRule<MethodBlockRule, BlockNode>();

        return new ConstructorDeclarationNode(methodName, parameters, body, modifiers);
    }
}