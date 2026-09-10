using Loom.Parser.AST;
using Loom.Parser.Tokenizer;

using static Loom.Parser.Tokenizer.Token;

namespace Loom.Parser.Rules.Default;

public record ModuleNode(Token Name, BlockNode Body) : ASTNode
{
    /// <inheritdoc/>
    public override IEnumerable<ASTNode> Children => [Body];
}

[ParserRule]
public class ModuleDeclarationRule : ParserRule<ModuleNode>
{
    /// <inheritdoc/>
    public ModuleDeclarationRule(LoomParser parser) : base(parser) { }
    
    /// <inheritdoc/>
    public override ModuleNode ParseNode()
    {
        Parser.Reader.Expect(TokenType.Module); // module
        var name = Parser.Reader.Expect(TokenType.Identifier); // name

        return new ModuleNode(name, Parser.GetRule<BlockRule>().ParseNode());
    }
}



