
namespace Loom.Parser.Tokenizer.Rules.Default;

[TokenizerRule]
public class CharacterRule : TokenizerRule
{
    private const char QUOTE = '\'';

    /// <inheritdoc />
    public override bool CanHandle(char current) => current == QUOTE || TokenRegistry.Characters.ContainsKey(current);

    /// <inheritdoc />
    public override Token Read()
    {
        if (Reader.PeekChar() == QUOTE)
            return ReadCharacterLiteral();

        var c = (char)Reader.Read();
        var next = Reader.PeekChar();

        if (TokenRegistry.TryGetMultiCharacterType($"{c}{next}", out var multiCharType))
        {
            Reader.Read();
            return CreateToken(multiCharType, $"{c}{next}");
        }

        TokenRegistry.TryGetCharacterType(c, out var type);
        return CreateToken(type, c.ToString());
    }

    /// <summary> Reads a character literal (e.g. <c>'a'</c>), keeping the source text verbatim so it can be decoded by the parser. </summary>
    private Token ReadCharacterLiteral()
    {
        var start = Reader.Position;
        var location = new TokenLocation(Reader.Line, Reader.Column);

        Reader.Read(); // '

        while (!Reader.IsEnd && Reader.PeekChar() != '\n')
        {
            var c = (char)Reader.Read();

            if (c == '\\' && !Reader.IsEnd)
                Reader.Read(); // The escaped character can never close the literal, not even a quote.
            else if (c == QUOTE)
                break;
        }

        return new Token(Token.TokenType.CharacterLiteral, Reader.Source[start..Reader.Position], location);
    }
}
