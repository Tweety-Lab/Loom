
namespace Loom.Parser.Tokenizer.Rules;

public abstract class TokenizerRule
{
    /// <summary> The reader this rule uses. </summary>
    public LoomStringReader Reader { get; set; } = null!;

    /// <summary> Checks if the current character can be handled by the rule. </summary>
    public abstract bool CanHandle(char current);

    /// <summary> Reads the token from the tokenizer. </summary>
    public abstract Token Read();

    /// <summary> Creates a token. </summary>
    protected Token CreateToken(Token.TokenType type, string value) => new Token(type, value, new TokenLocation(Reader.Line, Reader.Column));
}
