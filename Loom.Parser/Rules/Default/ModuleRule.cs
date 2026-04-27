using Loom.Parser.AST;

using static Loom.Parser.Tokenizer.Token;

namespace Loom.Parser.Rules.Default;

public record ModuleNode(IdentifierNameNode Name, BlockNode Body) : ASTNode
{
    /// <inheritdoc/>
    public override IEnumerable<ASTNode> Children => new[] { Body };
}

[ParserRule]
public class ModuleRule : ParserRule<ModuleNode>
{
    /// <inheritdoc/>
    public ModuleRule(LoomParser parser) : base(parser) { }
    
    /// <inheritdoc/>
    public override ModuleNode ParseNode()
    {
        Parser.Reader.Expect(TokenType.Module); // module
        var name = Parser.Reader.Expect(TokenType.Identifier); // name

        return new ModuleNode(new IdentifierNameNode(name), Parser.GetRule<BlockRule>().ParseNode());
    }
}
