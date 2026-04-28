using Loom.Parser.AST;
using Loom.Parser.Tokenizer;
using static Loom.Parser.Tokenizer.Token;

namespace Loom.Parser.Rules.Default;

public record MethodDeclarationNode(Token ReturnType, Token MethodName, BlockNode Body, List<Token> Modifiers) : ASTNode
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

        Token returnType = Parser.Reader.ExpectAny(TokenType.Void, TokenType.I32, TokenType.Identifier); // return type
        var methodName = Parser.Reader.Expect(TokenType.Identifier); // name

        Parser.Reader.Expect(TokenType.LParen); // (
        Parser.Reader.Expect(TokenType.RParen); // )

        return new MethodDeclarationNode(returnType, methodName, Parser.GetRule<MethodBlockRule>().ParseNode(), modifiers);
    }
}
