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

        while (!Parser.Reader.Check(TokenType.RBrace))
        {
            if (Parser.Reader.Check(TokenType.Module))
                body.Add(Parser.GetRule<ModuleRule>().Parse()); // nested modules
            else if (Parser.Reader.Check(TokenType.Unsafe))
                body.Add(Parser.GetRule<UnsafeRule>().Parse()); // unsafe
            else
                break;
        }

        Parser.Reader.Expect(TokenType.RBrace); // }
        return new BlockNode(body);
    }
}
