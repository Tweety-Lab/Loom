using Loom.Parser.AST;
using Loom.Parser.Tokenizer;
using static Loom.Parser.Tokenizer.Token;

namespace Loom.Parser.Rules.Default;

public record MethodDefinitionNode(string MethodName, BlockNode Body, List<Token> Modifiers) : ASTNode
{
    /// <inheritdoc/>
    public override IEnumerable<ASTNode> Children => new[] { Body };
}


[ParserRule]
public class MethodDefinitionRule : ParserRule<MethodDefinitionNode>
{
    /// <inheritdoc/>
    public MethodDefinitionRule(LoomParser parser) : base(parser) { }

    /// <inheritdoc/>
    public override MethodDefinitionNode Parse()
    {
        var modifiers = Parser.Reader.ExpectMany(t => TokenRegistry.IsModifier(t.Type));

        Parser.Reader.ExpectAny(TokenType.Void, TokenType.I32, TokenType.Identifier); // return type
        var methodName = Parser.Reader.Expect(TokenType.Identifier).Value; // name

        Parser.Reader.Expect(TokenType.LParen); // (
        Parser.Reader.Expect(TokenType.RParen); // )

        return new MethodDefinitionNode(methodName, Parser.GetRule<BlockRule>().Parse(), modifiers);
    }
}
