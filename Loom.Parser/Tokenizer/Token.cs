
namespace Loom.Parser.Tokenizer;

public class Token
{
    public enum TokenType
    {
        Identifier,

        LBrace,
        RBrace,

        Semicolon,

        Module,
        Import,
        Unsafe,

        Void,

        EOF
    }

    /// <summary> The tokenized type. </summary>
    public TokenType Type { get; set; }

    /// <summary> The tokenized string value. </summary>
    public string Value { get; set; }

    /// <summary> Initializes a new instance of the <see cref="Token"/> class. </summary>
    public Token(TokenType type, string value)
    {
        Type = type;
        Value = value;
    }
}
