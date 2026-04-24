using Loom.Parser.AST;

using static Loom.Parser.Tokenizer.Token;

namespace Loom.Parser.Rules.Default;

public record MethodDefinitionNode(string MethodName, BlockNode Body) : ASTNode
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
        Parser.Reader.Expect(TokenType.Void); // void
        var methodName = Parser.Reader.Expect(TokenType.Identifier).Value; // name

        Parser.Reader.Expect(TokenType.LParen); // (
        Parser.Reader.Expect(TokenType.RParen); // )

        return new MethodDefinitionNode(methodName, Parser.GetRule<BlockRule>().Parse());
    }
}
