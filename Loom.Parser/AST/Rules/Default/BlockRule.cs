using System.Diagnostics.CodeAnalysis;
using static Loom.Parser.Tokenizer.Token;

namespace Loom.Parser.AST.Rules.Default;

public record BlockNode(List<ASTNode> Contents) : ASTNode(Span.Cover(Contents))
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
        };

        ParseUntil(TokenType.RBrace, dispatch, () =>
        {
            var comments = Parser.Reader.Check(TokenType.DocumentComment) ? RunRule<DocumentCommentListRule, DocumentCommentListNode>() : null;

            if (TryParseMember(out var node))
            {
                if (node is ClassDeclarationNode declaration && comments is not null)
                    node = declaration with { DocumentComments = comments };

                body.Add(node);
            }
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
}
