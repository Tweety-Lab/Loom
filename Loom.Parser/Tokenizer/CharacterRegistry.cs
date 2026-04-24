using System.Reflection;

namespace Loom.Parser.Tokenizer;

public static class CharacterRegistry
{
    /// <summary> All currently registered character tokens. </summary>
    public static IReadOnlyDictionary<char, Token.TokenType> Characters => characters;

    private static Dictionary<char, Token.TokenType> characters = new();

    static CharacterRegistry()
    {
        foreach (var field in typeof(Token.TokenType).GetFields())
        {
            var attr = field.GetCustomAttribute<CharacterAttribute>();
            if (attr != null)
                characters[attr.Character] = (Token.TokenType)field.GetValue(null)!;
        }
    }

    /// <summary> Registers a new character token with its associated token type. </summary>
    public static void Register(char keyword, Token.TokenType type) => characters[keyword] = type;

    /// <summary> Tries to get the token type for a character. Returns false if it's not a character. </summary>
    public static bool TryGetCharacterType(char value, out Token.TokenType type) => characters.TryGetValue(value, out type);
}

