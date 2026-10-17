using Loom.Parser.Tokenizer;
using static Loom.Parser.Tokenizer.Token;

namespace Loom.Parser.AST.Rules.Default;

public record AttributeDeclarationNode(Token Name, ParameterListNode Parameters, List<Token> Modifiers) : ASTNode, IModifiableNode
{
    /// <inheritdoc/>
    public override IEnumerable<ASTNode> Children => [Parameters];
}

[ParserRule]
public class AttributeDeclarationRule : TypeMemberParserRule
{
    /// <inheritdoc/>
    public AttributeDeclarationRule(LoomParser parser) : base(parser) { }

    /// <inheritdoc/>
    public override AttributeDeclarationNode ParseNode()
    {
        var modifiers = Parser.Reader.ExpectMany(t => TokenRegistry.IsMemberModifier(t.Type));

        Parser.Reader.Expect(TokenType.Attribute);
        var name = Parser.Reader.Expect(TokenType.Identifier);
        var parameters = RunRule<ParameterListRule, ParameterListNode>();
        Parser.Reader.Expect(TokenType.Semicolon);

        return new AttributeDeclarationNode(name, parameters, modifiers);
    }
}
