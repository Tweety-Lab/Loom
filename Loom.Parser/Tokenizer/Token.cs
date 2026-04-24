
namespace Loom.Parser.Tokenizer;

/// <summary>
/// Marks a <see cref="Token.TokenType"/> as a modifier.
/// </summary>
[AttributeUsage(AttributeTargets.Field)]
public sealed class ModifierAttribute : Attribute { }

/// <summary>
/// Marks a <see cref="Token.TokenType"/> as a keyword.
/// </summary>
[AttributeUsage(AttributeTargets.Field)]
public sealed class KeywordAttribute : Attribute
{
    /// <summary> The associated string keyword. </summary>
    public string Keyword { get; }

    /// <summary> Initializes a new instance of the <see cref="KeywordAttribute"/> class. </summary>
    public KeywordAttribute(string keyword) => Keyword = keyword;
}

/// <summary>
/// Marks a <see cref="Token.TokenType"/> as a single character.
/// </summary>
[AttributeUsage(AttributeTargets.Field)]
public sealed class CharacterAttribute : Attribute
{
    /// <summary> The associated character. </summary>
    public char Character { get; }

    /// <summary> Initializes a new instance of the <see cref="CharacterAttribute"/> class. </summary>
    public CharacterAttribute(char character) => Character = character;
}

public class Token
{
    public enum TokenType
    {
        Identifier,

        [Character('{')] LBrace,
        [Character('}')] RBrace,

        [Character('(')] LParen,
        [Character(')')] RParen,

        [Character(';')] Semicolon,

        [Keyword("module")] Module,
        [Keyword("import")] Import,
        [Keyword("unsafe")] Unsafe,

        [Keyword("export"), Modifier] Export,

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
