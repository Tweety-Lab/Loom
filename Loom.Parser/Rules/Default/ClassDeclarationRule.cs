using Loom.Parser.AST;
using Loom.Parser.Tokenizer;
using static Loom.Parser.Tokenizer.Token;

namespace Loom.Parser.Rules.Default;

public record ClassDeclarationNode(Token Name, BlockNode Body, List<Token> Modifiers) : ASTNode, ITypeDeclarationNode
{
    /// <inheritdoc/>
    public override IEnumerable<ASTNode> Children => [Body];
}


[ParserRule]
public class ClassDeclarationRule : ParserRule<ClassDeclarationNode>
{
    /// <inheritdoc/>
    public ClassDeclarationRule(LoomParser parser) : base(parser) { }

    /// <inheritdoc/>
    public override ClassDeclarationNode ParseNode()
    {
        var modifiers = Parser.Reader.ExpectMany(t => TokenRegistry.IsMemberModifier(t.Type));

        Parser.Reader.Expect(TokenType.Class); // class

        var className = Parser.Reader.Expect(TokenType.Identifier); // name

        BlockNode body = RunRule<TypeBlockRule, BlockNode>();

        return new ClassDeclarationNode(className, body, modifiers);
    }
}
