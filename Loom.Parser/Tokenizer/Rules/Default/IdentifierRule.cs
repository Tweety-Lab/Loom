using System.Text;

namespace Loom.Parser.Tokenizer.Rules.Default;

[TokenizerRule]
public class IdentifierRule : ITokenizerRule
{
    /// <inheritdoc />
    public bool CanHandle(char current) => char.IsLetter(current) || current == '_';

    /// <inheritdoc />
    public Token Read(StringReader reader)
    {
        var sb = new StringBuilder();

        while (reader.Peek() != -1 && (char.IsLetterOrDigit((char)reader.Peek()) || (char)reader.Peek() == '_'))
            sb.Append((char)reader.Read());

        var value = sb.ToString();
        var type = KeywordRegistry.TryGetKeywordType(value, out var keywordType) ? keywordType : Token.TokenType.Identifier;

        return new Token(type, value);
    }
}
