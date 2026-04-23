namespace Loom.Parser.Tokenizer.Rules.Default;

[TokenizerRule]
public class LParenRule : ITokenizerRule
{
    /// <inheritdoc />
    public bool CanHandle(char current) => current == '{';

    /// <inheritdoc />
    public Token Read(StringReader reader)
    {
        reader.Read();
        return new Token(Token.TokenType.LBrace, "{");
    }
}

[TokenizerRule]
public class RParenRule : ITokenizerRule
{
    /// <inheritdoc />
    public bool CanHandle(char current) => current == '}';

    /// <inheritdoc />
    public Token Read(StringReader reader)
    {
        reader.Read();
        return new Token(Token.TokenType.RBrace, "}");
    }
}