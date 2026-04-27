using System.Text;

namespace Loom.Parser.Tokenizer.Rules.Default;

[TokenizerRule]
public class IdentifierRule : TokenizerRule
{
    /// <inheritdoc />
    public override bool CanHandle(char current) => char.IsLetter(current) || current == '_';

    /// <inheritdoc />
    public override Token Read()
    {
        var sb = new StringBuilder();

        while (Reader.Peek() != -1 && (char.IsLetterOrDigit(Reader.PeekChar()) || Reader.PeekChar() == '_'))
            sb.Append((char)Reader.Read());

        var value = sb.ToString();
        var type = TokenRegistry.TryGetKeywordType(value, out var keywordType) ? keywordType : Token.TokenType.Identifier;

        return CreateToken(type, value);
    }
}
