using System.Text;

namespace Loom.Parser.Tokenizer.Rules.Default;

[TokenizerRule]
public class IdentifierRule : ITokenizerRule
{
    /// <inheritdoc />
    public bool CanHandle(char current) => char.IsLetter(current) || current == '_';

    /// <inheritdoc />
    public Token Read(LoomStringReader reader)
    {
        var sb = new StringBuilder();

        while (reader.Peek() != -1 && (char.IsLetterOrDigit(reader.PeekChar()) || reader.PeekChar() == '_'))
            sb.Append((char)reader.Read());

        var value = sb.ToString();
        var type = TokenRegistry.TryGetKeywordType(value, out var keywordType) ? keywordType : Token.TokenType.Identifier;

        return new Token(type, value);
    }
}
