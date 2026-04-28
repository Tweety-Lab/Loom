using Loom.Parser.AST;
using Loom.Parser.Tokenizer;
using System.ComponentModel;
using static Loom.Parser.Tokenizer.Token;
using static System.Reflection.Metadata.BlobBuilder;

namespace Loom.Parser.Rules.Default;

public record BlockNode(List<ASTNode> Contents) : ASTNode
{
    /// <inheritdoc/>
    public override IEnumerable<ASTNode> Children => Contents;
}

[ParserRule]
public class ModuleBlockRule : ParserRule<BlockNode>
{
    /// <inheritdoc/>
    public ModuleBlockRule(LoomParser parser) : base(parser) { }

    /// <inheritdoc/>
    public override BlockNode ParseNode()
    {
        var body = new List<ASTNode>();
        Parser.Reader.Expect(TokenType.LBrace); // {

        var dispatch = new Dictionary<TokenType, Action>
        {
            [TokenType.Module] = () => body.Add(RunRule<ModuleRule, ModuleNode>())
        };

        foreach (var modifier in TokenRegistry.Modifiers)
            dispatch[modifier] = () => body.Add(RunRule<MethodDefinitionRule, MethodDefinitionNode>());

        ParseUntil(TokenType.RBrace, dispatch, () => body.Add(RunRule<MethodDefinitionRule, MethodDefinitionNode>()));

        Parser.Reader.Expect(TokenType.RBrace); // }

        return new BlockNode(body);
    }
}

[ParserRule]
public class MethodBlockRule : ParserRule<BlockNode>
{
    /// <inheritdoc/>
    public MethodBlockRule(LoomParser parser) : base(parser) { }

    /// <inheritdoc/>
    public override BlockNode ParseNode()
    {
        var body = new List<ASTNode>();
        Parser.Reader.Expect(TokenType.LBrace); // {

        var dispatch = new Dictionary<TokenType, Action>
        {
            [TokenType.Unsafe] = () => body.Add(RunRule<UnsafeRule, UnsafeNode>()),
        };

        ParseUntil(TokenType.RBrace, dispatch, () => body.Add(RunRule<StatementRule, StatementNode>()));

        Parser.Reader.Expect(TokenType.RBrace); // }

        return new BlockNode(body);
    }
}




