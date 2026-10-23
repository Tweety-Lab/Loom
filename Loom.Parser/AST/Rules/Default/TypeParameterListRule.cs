using Loom.Parser.Tokenizer;
using static Loom.Parser.Tokenizer.Token;

namespace Loom.Parser.AST.Rules.Default;

public record TypeParameterNode(Token Token) : ASTNode(Span.FromToken(Token))
{
    public string BaseName => Token.Text;

    /// <inheritdoc/>
    public override IEnumerable<ASTNode> Children => Enumerable.Empty<ASTNode>();
}

public record TypeParameterListNode : ListNode<TypeParameterNode>
{
    /// <summary> Initializes a new instance of the <see cref="TypeParameterListNode"/> class. </summary>
    public TypeParameterListNode(List<TypeParameterNode> typeParameters) : base(typeParameters) { }
}

[ParserRule]
public class TypeParameterListRule : ParserRule<TypeParameterListNode>
{
    /// <inheritdoc/>
    public TypeParameterListRule(LoomParser parser) : base(parser) { }

    /// <inheritdoc/>
    public override TypeParameterListNode ParseNode()
    {
        Parser.Reader.Expect(TokenType.Less); // <

        var typeParameters = new List<TypeParameterNode>();
        if (Parser.Reader.Peek(0).Type != TokenType.Greater)
        {
            do
            {
                var name = Parser.Reader.Expect(TokenType.Identifier); // name

                typeParameters.Add(new TypeParameterNode(name));
            }
            while (Parser.Reader.Match(TokenType.Comma));
        }

        Parser.Reader.Expect(TokenType.Greater); // >

        return new TypeParameterListNode(typeParameters);
    }

    /// <summary> Skips a <c>&lt;...&gt;</c> type parameter list if one starts at <paramref name="offset"/>. </summary>
    public static int SkipTypeParameters(TokenReader reader, int offset)
    {
        if (reader.Peek(offset).Type != TokenType.Less)
            return offset;

        var start = offset;
        offset++;

        while (reader.Peek(offset).Type == TokenType.Identifier)
        {
            offset++;

            if (reader.Peek(offset).Type != TokenType.Comma)
                break;

            offset++;
        }

        if (reader.Peek(offset).Type != TokenType.Greater)
            return start;

        return offset + 1;
    }
}
