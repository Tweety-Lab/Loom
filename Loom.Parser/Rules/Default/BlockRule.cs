using Loom.Parser.AST;
using Loom.Parser.Tokenizer;
using static Loom.Parser.Tokenizer.Token;
using System.Diagnostics.CodeAnalysis;

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
            if (TryParseDeclaration(out var node))
                body.Add(node);
            else
                body.Add(RunRule<StatementRule, StatementNode>());
        });

        Parser.Reader.Expect(TokenType.RBrace); // }
        return new BlockNode(body);
    }

    private bool TryParseDeclaration([NotNullWhen(true)] out ASTNode? node)
    {
        var offset = 0;
        while (TokenRegistry.IsModifier(Parser.Reader.Peek(offset).Type))
            offset++;

        if (Parser.Reader.Peek(offset).Type == TokenType.Struct)
        {
            node = RunRule<StructDeclarationRule, StructDeclarationNode>();
            return true;
        }

        if (IsTypeName(Parser.Reader.Peek(offset).Type)
            && Parser.Reader.Peek(offset + 1).Type == TokenType.Identifier
            && Parser.Reader.Peek(offset + 2).Type == TokenType.LParen)
        {
            node = RunRule<MethodDeclarationRule, MethodDeclarationNode>();
            return true;
        }

        node = null;
        return false;

        static bool IsTypeName(TokenType type) => type == TokenType.Identifier || TokenRegistry.IsBuiltInType(type);
    }
}