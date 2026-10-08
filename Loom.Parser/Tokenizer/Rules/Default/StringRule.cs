namespace Loom.Parser.Tokenizer.Rules.Default;

[TokenizerRule]
public class StringRule : TokenizerRule
{
    private const char QUOTE = '"';

    /// <inheritdoc />
    public override bool CanHandle(char current) => current == QUOTE;

    /// <inheritdoc />
    public override Token Read()
    {
        var start = Reader.Position;
        var location = new TokenLocation(Reader.Line, Reader.Column);

        Reader.Read(); // "

        while (!Reader.IsEnd && Reader.PeekChar() != '\n')
        {
            var c = (char)Reader.Read();

            if (c == '\\' && !Reader.IsEnd)
                Reader.Read(); // an escaped character can never close the literal

            else if (c == QUOTE)
                break;
        }

        return new Token(Token.TokenType.StringLiteral, Reader.Source[start..Reader.Position], location);
    }
}