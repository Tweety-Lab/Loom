using Loom.Parser.Tokenizer;
using static Loom.Parser.Tokenizer.Token;

namespace Loom.Parser.AST.Rules.Default;

public record PropertyDeclarationNode(List<Token> Modifiers, TypeNode Type, Token Name, List<AccessorNode> Accessors, ExpressionNode? Initializer) : ASTNode, IModifiableNode
{
    // TODO: Visit the accessor bodies once declared accessors are supported instead of being auto-implemented
    /// <inheritdoc/>
    public override IEnumerable<ASTNode> Children => Initializer is not null ? [Initializer] : Enumerable.Empty<ASTNode>();
}

public record AccessorNode(Token AccessorType, BlockNode? Body) : ASTNode
{
    /// <inheritdoc/>
    public override IEnumerable<ASTNode> Children => Body is not null ? [Body] : Enumerable.Empty<ASTNode>();
}

[ParserRule]
public class PropertyDeclarationRule : TypeMemberParserRule
{
    /// <inheritdoc/>
    public PropertyDeclarationRule(LoomParser parser) : base(parser) { }

    /// <inheritdoc/>
    public override PropertyDeclarationNode ParseNode()
    {
        var modifiers = Parser.Reader.ExpectMany(t => TokenRegistry.IsMemberModifier(t.Type)); // modifiers

        var type = RunRule<TypeRule, TypeNode>(); // type
        var name = Parser.Reader.Expect(TokenType.Identifier); // name

        Parser.Reader.Expect(TokenType.LBrace); // {

        var accessors = new List<AccessorNode>();
        while (!Parser.Reader.Check(TokenType.RBrace))
            accessors.Add(RunRule<AccessorRule, AccessorNode>()); // get or set

        Parser.Reader.Expect(TokenType.RBrace); // }

        ExpressionNode? initializer = null;
        if (Parser.Reader.Match(TokenType.Equals)) // =
        {
            initializer = RunRule<ExpressionRule, ExpressionNode>(); // initializer
            Parser.Reader.Expect(TokenType.Semicolon); // ;
        }

        return new PropertyDeclarationNode(modifiers, type, name, accessors, initializer);
    }
}

[ParserRule]
public class AccessorRule : ParserRule<AccessorNode>
{
    /// <inheritdoc/>
    public AccessorRule(LoomParser parser) : base(parser) { }

    /// <inheritdoc/>
    public override AccessorNode ParseNode()
    {
        var accessorType = Parser.Reader.ExpectAny(t => t.Type is TokenType.Get or TokenType.Set); // get or set

        BlockNode? body = null;
        if (!Parser.Reader.Match(TokenType.Semicolon)) // ;
            body = RunRule<MethodBlockRule, BlockNode>(); // { ... }

        return new AccessorNode(accessorType, body);
    }
}
