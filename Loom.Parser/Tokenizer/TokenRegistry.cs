using System.Reflection;

namespace Loom.Parser.Tokenizer;

/// <summary> Metadata for registered tokens. </summary>
public static class TokenRegistry
{
    /// <summary> All currently registered keywords. </summary>
    public static IReadOnlyDictionary<string, Token.TokenType> Keywords => keywords;

    /// <summary> All currently registered characters. </summary>
    public static IReadOnlyDictionary<char, Token.TokenType> Characters => characters;

    /// <summary> All currently registered modifiers. </summary>
    public static IReadOnlySet<Token.TokenType> Modifiers => modifiers;

    private static readonly Dictionary<string, Token.TokenType> keywords = new();
    private static readonly Dictionary<char, Token.TokenType> characters = new();
    private static readonly HashSet<Token.TokenType> modifiers = new();

    static TokenRegistry()
    {
        foreach (var field in typeof(Token.TokenType).GetFields(BindingFlags.Public | BindingFlags.Static))
        {
            var type = (Token.TokenType)field.GetValue(null)!;

            if (field.GetCustomAttribute<KeywordAttribute>() is { } kw)
                keywords[kw.Keyword] = type;

            if (field.GetCustomAttribute<CharacterAttribute>() is { } ch)
                characters[ch.Character] = type;

            if (field.IsDefined(typeof(ModifierAttribute)))
                modifiers.Add(type);
        }
    }

    public static bool TryGetKeywordType(string value, out Token.TokenType type) => keywords.TryGetValue(value, out type);

    public static bool TryGetCharacterType(char value, out Token.TokenType type) => characters.TryGetValue(value, out type);

    public static bool IsModifier(Token.TokenType type) => modifiers.Contains(type);
}
