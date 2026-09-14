using System.Diagnostics.CodeAnalysis;
using Loom.Parser.AST;
using Loom.Parser.Tokenizer;
using static Loom.Parser.Tokenizer.Token;

namespace Loom.Parser.Rules.Default;

public record BlockNode(List<ASTNode> Contents) : ASTNode
{
    /// <inheritdoc/>
    public override IEnumerable<ASTNode> Children => Contents;
}

/// <summary> Base rule for parsing a <c>{ ... }</c> body. </summary>
public abstract class BlockRule : ParserRule<BlockNode>
{
    /// <inheritdoc/>
    protected BlockRule(LoomParser parser) : base(parser) { }

    /// <inheritdoc/>
    public override BlockNode ParseNode()
    {
        var body = new List<ASTNode>();
        Parser.Reader.Expect(TokenType.LBrace); // {

        var dispatch = new Dictionary<TokenType, Action>
        {
            [TokenType.Module] = () => body.Add(RunRule<ModuleDeclarationRule, ModuleNode>()),
            [TokenType.Unsafe] = () => body.Add(RunRule<UnsafeRule, UnsafeNode>()),
        };

        ParseUntil(TokenType.RBrace, dispatch, () =>
        {
            if (TryParseMember(out var node))
                body.Add(node);
            else
                body.Add(RunRule<StatementRule, StatementNode>());
        });

        Parser.Reader.Expect(TokenType.RBrace); // }
        return new BlockNode(body);
    }

    /// <summary> Attempts to parse a member declaration valid in this block context. Returns <see langword="false"/> to fall back to statement parsing. </summary>
    protected virtual bool TryParseMember([NotNullWhen(true)] out ASTNode? node)
    {
        node = null;
        return false;
    }

    protected static bool IsTypeName(TokenType type) => type == TokenType.Identifier || TokenRegistry.IsBuiltInType(type);
}