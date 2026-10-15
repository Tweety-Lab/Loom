using Loom.Parser.AST;
using Loom.Parser.AST.Rules;
using Loom.Parser.Tokenizer;
using static Loom.Parser.Tokenizer.Token;

namespace Loom.Parser.AST.Rules.Default;

public record TypeArgumentListNode : ListNode<TypeNode>
{
    /// <summary> Initializes a new instance of the <see cref="TypeArgumentListNode"/> class. </summary>
    public TypeArgumentListNode(List<TypeNode> typeArguments) : base(typeArguments) { }
}

[ParserRule]
public class TypeArgumentListRule : ParserRule<TypeArgumentListNode>
{
    /// <inheritdoc/>
    public TypeArgumentListRule(LoomParser parser) : base(parser) { }

    /// <inheritdoc/>
    public override TypeArgumentListNode ParseNode()
    {
        Parser.Reader.Expect(TokenType.Less); // <

        var typeArguments = new List<TypeNode>();
        if (Parser.Reader.Peek(0).Type != TokenType.Greater)
        {
            do
            {
                typeArguments.Add(RunRule<TypeRule, TypeNode>()); // type
            }
            while (Parser.Reader.Match(TokenType.Comma));
        }

        Parser.Reader.Expect(TokenType.Greater); // >

        return new TypeArgumentListNode(typeArguments);
    }

    /// <summary> Skips a <c>&lt;...&gt;</c> type argument list if one starts at <paramref name="offset"/>. </summary>
    public static int SkipTypeArguments(TokenReader reader, int offset)
    {
        if (reader.Peek(offset).Type != TokenType.Less)
            return offset;

        var start = offset;
        offset++;

        while (TypeRule.IsTypeName(reader.Peek(offset).Type))
        {
            offset = TypeRule.SkipType(reader, offset);

            if (reader.Peek(offset).Type != TokenType.Comma)
                break;

            offset++;
        }

        if (reader.Peek(offset).Type != TokenType.Greater)
            return start;

        return offset + 1;
    }
}
