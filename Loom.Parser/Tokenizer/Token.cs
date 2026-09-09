
namespace Loom.Parser.Tokenizer;

/// <summary>
/// Marks a <see cref="Token.TokenType"/> as a modifier.
/// </summary>
[AttributeUsage(AttributeTargets.Field)]
public sealed class ModifierAttribute : Attribute { }

/// <summary>
/// Marks a <see cref="Token.TokenType"/> as a built-in type.
/// </summary>
[AttributeUsage(AttributeTargets.Field)]
public sealed class TypeAttribute : Attribute { }

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

public readonly struct TokenLocation
{
    public int Line { get; }
    public int Column { get; }

    /// <summary> Initializes a new instance of the <see cref="TokenLocation"/> class. </summary>
    public TokenLocation(int line, int column)
    {
        Line = line;
        Column = column;
    }

    /// <inheritdoc/>
    public override string ToString() => $"({Line}, {Column})";
}

public class Token
{
    public enum TokenType
    {
        Identifier,
        Number,

        [Character('{')] LBrace,
        [Character('}')] RBrace,

        [Character('(')] LParen,
        [Character(')')] RParen,

        [Character('=')] Equals,
        [Character('+')] Plus,
        [Character('-')] Minus,
        [Character('*')] Star,
        [Character('/')] Slash,

        [Character('!')] Exclamation,
        [Character('<')] Less,
        [Character('>')] Greater,

        EqualEqual,
        NotEqual,
        LessEqual,
        GreaterEqual,

        [Character(',')] Comma,
        [Character(';')] Semicolon,

        [Keyword("module")] Module,
        [Keyword("import")] Import,
        [Keyword("unsafe")] Unsafe,

        [Keyword("export"), Modifier] Export,
        [Keyword("extern"), Modifier] Extern,

        [Keyword("true")] True,
        [Keyword("false")] False,

        [Keyword("if")] If,

        [Keyword("return")] Return,

        [Keyword("void"), Type] Void,
        [Keyword("i32"), Type] I32,
        [Keyword("bool"), Type] Bool,

        EOF
    }

    /// <summary> The tokenized type. </summary>
    public TokenType Type { get; }

    /// <summary> The tokenized string value. </summary>
    public string Text { get; }
    
    /// <summary> The source location of the token. </summary>
    public TokenLocation Location { get; }

    /// <summary> Initializes a new instance of the <see cref="Token"/> class. </summary>
    public Token(TokenType type, string value, TokenLocation location)
    {
        Type = type;
        Text = value;
        Location = location;
    }
}
