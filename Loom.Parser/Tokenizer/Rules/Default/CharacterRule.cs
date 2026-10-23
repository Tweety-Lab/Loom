
namespace Loom.Parser.Tokenizer.Rules.Default;

[TokenizerRule]
public class CharacterRule : TokenizerRule
{
    private const char QUOTE = '\'';
    private static readonly KeyValuePair<string, Token.TokenType>[] MultiCharacters = [.. TokenRegistry.MultiCharacters.OrderByDescending(pair => pair.Key.Length)];

    /// <inheritdoc />
    public override bool CanHandle(char current) => (current == QUOTE || TokenRegistry.Characters.ContainsKey(current)) && !(current == '/' && Reader.Peek(1) == '/' && Reader.Peek(2) == '/');

    /// <inheritdoc />
    public override Token Read()
    {
        if (Reader.PeekChar() == QUOTE)
            return ReadCharacterLiteral();

        foreach (var (text, multiCharType) in MultiCharacters)
        {
            if (!Reader.Source.AsSpan(Reader.Position).StartsWith(text, StringComparison.Ordinal))
                continue;

            for (var i = 0; i < text.Length; i++)
                Reader.Read();

            return CreateToken(multiCharType, text);
        }

        var c = (char)Reader.Read();
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
                Reader.Read(); // an escaped character can never close the literal

            else if (c == QUOTE)
                break;
        }

        return new Token(Token.TokenType.CharacterLiteral, Reader.Source[start..Reader.Position], location);
    }
}
