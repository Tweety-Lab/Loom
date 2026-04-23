using Loom.Parser.AST;

using static Loom.Parser.Tokenizer.Token;

namespace Loom.Parser.Rules.Default;

[ParserRule]
public class UnsafeRule : ParserRule<UnsafeNode>
{
    /// <inheritdoc/>
    public UnsafeRule(LoomParser parser) : base(parser) { }

    /// <inheritdoc/>
    public override UnsafeNode Parse()
    {
        Parser.Reader.Expect(TokenType.Unsafe); // unsafe

        return new UnsafeNode(Parser.GetRule<BlockRule>().Parse());
    }
}
