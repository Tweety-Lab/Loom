using Loom.Parser.AST;

using static Loom.Parser.Tokenizer.Token;

namespace Loom.Parser.Rules.Default;

[ParserRule]
public class BlockRule : ParserRule<BlockNode>
{
    /// <inheritdoc/>
    public BlockRule(LoomParser parser) : base(parser) { }

    /// <inheritdoc/>
    public override BlockNode Parse()
    {
        var body = new List<ASTNode>();
        Parser.Reader.Expect(TokenType.LBrace); // {

        ParseUntil(TokenType.RBrace, new()
        {
            [TokenType.Module] = () => body.Add(RunRule<ModuleRule, ModuleNode>()), // Nested Modules
            [TokenType.Unsafe] = () => body.Add(RunRule<UnsafeRule, UnsafeNode>()), // Unsafe
        });

        Parser.Reader.Expect(TokenType.RBrace); // }

        return new BlockNode(body);
    }
}
