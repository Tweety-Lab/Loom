using Loom.Parser.AST;
using Loom.Parser.Tokenizer;
using static Loom.Parser.Tokenizer.Token;

namespace Loom.Parser.Rules.Default;

public record ArraySizeSpecifierNode(Token? Size) : ASTNode
{
    /// <inheritdoc/>
    public override IEnumerable<ASTNode> Children => Enumerable.Empty<ASTNode>();
}

public record TypeNode(Token Base, List<Token> Modifiers, ArraySizeSpecifierNode? ArraySize = null) : ASTNode, IModifiableNode
{
    /// <inheritdoc/>
    public override IEnumerable<ASTNode> Children => Enumerable.Empty<ASTNode>();
}

// Extensions to help with type parsing
// TODO: Move?
public static class TypeSyntax
{
    public static bool IsTypeName(TokenType type) => type == TokenType.Identifier || TokenRegistry.IsBuiltInType(type);

    public static int SkipModifiers(TokenReader reader, int offset)
    {
        while (TokenRegistry.IsModifier(reader.Peek(offset).Type))
            offset++;

        return offset;
    }

    public static int SkipType(TokenReader reader, int offset)
    {
        if (!IsTypeName(reader.Peek(offset).Type))
            return offset;

        offset++;

        if (reader.Peek(offset).Type == TokenType.LBracket)
        {
            offset++;

            if (reader.Peek(offset).Type == TokenType.Number)
                offset++;

            if (reader.Peek(offset).Type == TokenType.RBracket)
                offset++;
        }

        return offset;
    }
}

[ParserRule]
public class TypeRule : ParserRule<TypeNode>
{
    /// <inheritdoc/>
    public TypeRule(LoomParser parser) : base(parser) { }

    /// <inheritdoc/>
    public override TypeNode ParseNode()
    {
        var modifiers = Parser.Reader.ExpectMany(t => TokenRegistry.IsTypeModifier(t.Type));

        var type = Parser.Reader.ExpectAny(t => TypeSyntax.IsTypeName(t.Type)); // type

        ArraySizeSpecifierNode? arraySize = null;

        if (Parser.Reader.Match(TokenType.LBracket)) // [
        {
            var size = Parser.Reader.Expect(TokenType.Number); // number
            Parser.Reader.Expect(TokenType.RBracket); // ]

            arraySize = new ArraySizeSpecifierNode(size);
        }

        return new TypeNode(type, modifiers, arraySize);
    }
}
