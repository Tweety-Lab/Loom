using Loom.Parser.AST;
using Loom.Parser.Tokenizer;
using static Loom.Parser.Tokenizer.Token;

namespace Loom.Parser.Rules.Default;

public record ParameterNode(Token Type, Token Name) : ASTNode
{
    public override IEnumerable<ASTNode> Children => [];
}

public record MethodDeclarationNode(Token ReturnType, Token MethodName, List<ParameterNode> Parameters, BlockNode Body, List<Token> Modifiers) : ASTNode
{
    /// <inheritdoc/>
    public override IEnumerable<ASTNode> Children => [Body];
}


[ParserRule]
public class MethodDeclarationRule : ParserRule<MethodDeclarationNode>
{
    /// <inheritdoc/>
    public MethodDeclarationRule(LoomParser parser) : base(parser) { }

    /// <inheritdoc/>
    public override MethodDeclarationNode ParseNode()
    {
        var modifiers = Parser.Reader.ExpectMany(t => TokenRegistry.IsModifier(t.Type));

        Token returnType = Parser.Reader.ExpectAny(t => TokenRegistry.IsBuiltInType(t.Type)); // return type
        var methodName = Parser.Reader.Expect(TokenType.Identifier); // name

        Parser.Reader.Expect(TokenType.LParen); // (

        // Paramaters
        var parameters = new List<ParameterNode>();
        if (Parser.Reader.Peek(0).Type != TokenType.RParen)
        {
            do
            {
                var type = Parser.Reader.ExpectAny(t => TokenRegistry.IsBuiltInType(t.Type));
                var name = Parser.Reader.Expect(TokenType.Identifier);

                parameters.Add(new ParameterNode(type, name));
            }
            while (Parser.Reader.Match(TokenType.Comma));
        }

        Parser.Reader.Expect(TokenType.RParen); // )

        
        bool needsBody = true;
        if (modifiers.Any(m => m.Type == TokenType.Extern))
            needsBody = false;

        BlockNode body = new BlockNode(new List<ASTNode>());
        if (needsBody)
            body = Parser.GetRule<BlockRule>().ParseNode();
        else
            Parser.Reader.Expect(TokenType.Semicolon); // ;

        return new MethodDeclarationNode(returnType, methodName, parameters, body, modifiers);
    }
}