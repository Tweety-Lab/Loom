using System.Text;

namespace Loom.Parser.Tokenizer.Rules.Default;

[TokenizerRule]
public class NumberRule : ITokenizerRule
{
    /// <inheritdoc />
    public bool CanHandle(char current) => char.IsDigit(current);

    /// <inheritdoc />
    public Token Read(LoomStringReader reader)
    {
        var sb = new StringBuilder();

        while (reader.Peek() != -1 && char.IsDigit(reader.PeekChar()))
            sb.Append((char)reader.Read());

        return new Token(Token.TokenType.Number, sb.ToString());
    }
}

