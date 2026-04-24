
using System.Reflection;

namespace Loom.Parser.Tokenizer;

public static class KeywordRegistry
{
    /// <summary> All currently registered keywords. </summary>
    public static IReadOnlyDictionary<string, Token.TokenType> Keywords => keywords;

    private static Dictionary<string, Token.TokenType> keywords = new();

    static KeywordRegistry()
    {
        foreach (var field in typeof(Token.TokenType).GetFields())
        {
            var attr = field.GetCustomAttribute<KeywordAttribute>();
            if (attr != null)
                keywords[(string)attr.Keyword] = (Token.TokenType)field.GetValue(null)!;
        }
    }

    /// <summary> Registers a new keyword with its associated token type. </summary>
    public static void Register(string keyword, Token.TokenType type) => keywords[keyword] = type;

    /// <summary> Tries to get the token type for a keyword. Returns false if it's not a keyword. </summary>
    public static bool TryGetKeywordType(string value, out Token.TokenType type) => keywords.TryGetValue(value, out type);
}
