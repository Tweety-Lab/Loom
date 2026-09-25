using Loom.Parser.AST;
using Loom.Parser.Tokenizer;
using static Loom.Parser.Tokenizer.Token;

namespace Loom.Parser.Rules.Default;

public record TypeNode(Token Base) : ASTNode
{
    /// <inheritdoc/>
    public override IEnumerable<ASTNode> Children => Enumerable.Empty<ASTNode>();
}

[ParserRule]
public class TypeRule : ParserRule<TypeNode>
{
    /// <inheritdoc/>
    public TypeRule(LoomParser parser) : base(parser) { }

    /// <inheritdoc/>
    public override TypeNode ParseNode()
    {
        var type = Parser.Reader.ExpectAny(t => TokenRegistry.IsBuiltInType(t.Type) || t.Type == TokenType.Identifier); // type

        return new TypeNode(type);
    }
}
