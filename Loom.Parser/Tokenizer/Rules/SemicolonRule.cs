
namespace Loom.Parser.Tokenizer.Rules;

[TokenizerRule]
public class SemicolonRule : ITokenizerRule
{
    /// <inheritdoc />
    public bool CanHandle(char current) => current == ';';

    /// <inheritdoc />
    public Token Read(StringReader reader)
    {
        reader.Read();
        return new Token(Token.TokenType.Semicolon, ";");
    }
}
