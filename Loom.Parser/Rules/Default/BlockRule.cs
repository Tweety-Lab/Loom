using Loom.Parser.AST;
using Loom.Parser.Tokenizer;
using static Loom.Parser.Tokenizer.Token;

namespace Loom.Parser.Rules.Default;

public record BlockNode(List<ASTNode> Contents) : ASTNode
{
    /// <inheritdoc/>
    public override IEnumerable<ASTNode> Children => Contents;
}

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

        var dispatch = new Dictionary<TokenType, Action>
        {
            [TokenType.Void] = () => body.Add(RunRule<MethodDefinitionRule, MethodDefinitionNode>()),
            [TokenType.I32] = () => body.Add(RunRule<MethodDefinitionRule, MethodDefinitionNode>()),
            [TokenType.Identifier] = () => body.Add(RunRule<MethodDefinitionRule, MethodDefinitionNode>()), // Method Definitions HACK

            [TokenType.Module] = () => body.Add(RunRule<ModuleRule, ModuleNode>()), // Nested Modules
            [TokenType.Unsafe] = () => body.Add(RunRule<UnsafeRule, UnsafeNode>()), // Unsafe
        };

        foreach (var modifier in TokenRegistry.Modifiers)
            dispatch[modifier] = () => body.Add(RunRule<MethodDefinitionRule, MethodDefinitionNode>()); // Keywords

        ParseUntil(TokenType.RBrace, dispatch, () => body.Add(RunRule<StatementRule, StatementNode>()));

        Parser.Reader.Expect(TokenType.RBrace); // }

        return new BlockNode(body);
    }
}
