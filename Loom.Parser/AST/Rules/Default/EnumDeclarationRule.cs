using Loom.Parser.Tokenizer;
using static Loom.Parser.Tokenizer.Token;

namespace Loom.Parser.AST.Rules.Default;

public record EnumMemberNode(Token Name) : ASTNode(Span.FromToken(Name))
{
    /// <inheritdoc/>
    public override IEnumerable<ASTNode> Children => Enumerable.Empty<ASTNode>();
}

public record EnumDeclarationNode(Token Name, BlockNode Body, List<Token> Modifiers) : ASTNode, ITypeDeclarationNode
{
    /// <inheritdoc/>
    public override IEnumerable<ASTNode> Children => [Body];
}

[ParserRule]
public class EnumDeclarationRule : TypeMemberParserRule
{
    /// <inheritdoc/>
    public EnumDeclarationRule(LoomParser parser) : base(parser) { }

    /// <inheritdoc/>
    public override EnumDeclarationNode ParseNode()
    {
        var modifiers = Parser.Reader.ExpectMany(t => TokenRegistry.IsMemberModifier(t.Type));

        Parser.Reader.Expect(TokenType.Enum);
        var name = Parser.Reader.Expect(TokenType.Identifier);
        var bodyStart = Parser.Reader.Position;
        Parser.Reader.Expect(TokenType.LBrace);

        var members = new List<ASTNode>();
        while (!Parser.Reader.Check(TokenType.RBrace))
        {
            var memberName = Parser.Reader.Expect(TokenType.Identifier);
            members.Add(new EnumMemberNode(memberName));

            if (!Parser.Reader.Check(TokenType.RBrace))
                Parser.Reader.Expect(TokenType.Comma);
        }

        Parser.Reader.Expect(TokenType.RBrace);
        return new EnumDeclarationNode(name, Parser.Complete(new BlockNode(members), bodyStart), modifiers);
    }
}
