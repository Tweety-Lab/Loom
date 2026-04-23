namespace Loom.Parser.Tokenizer.Rules.Default;

[TokenizerRule]
public class SemicolonRule : ITokenizerRule
{
    /// <inheritdoc />
    public bool CanHandle(char current) => current == ';';

    /// <inheritdoc />
    public Token Read(LoomStringReader reader)
    {
        reader.Read();
        return new Token(Token.TokenType.Semicolon, ";");
    }
}
