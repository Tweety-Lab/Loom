
namespace Loom.Parser.Tokenizer;

/// <summary>
/// Marks a <see cref="Token.TokenType"/> as a keyword.
/// </summary>
[AttributeUsage(AttributeTargets.Field)]
public class KeywordAttribute : Attribute
{
    /// <summary> The associated string keyword. </summary>
    public string Keyword { get; }

    /// <summary> Initializes a new instance of the <see cref="KeywordAttribute"/> class. </summary>
    public KeywordAttribute(string keyword) => Keyword = keyword;
}

public class Token
{
    public enum TokenType
    {
        Identifier,

        LBrace,
        RBrace,

        Semicolon,

        [Keyword("module")] Module,
        [Keyword("import")] Import,
        [Keyword("unsafe")] Unsafe,

        [Keyword("void")] Void,

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
