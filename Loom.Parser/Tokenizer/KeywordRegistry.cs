
namespace Loom.Parser.Tokenizer;

// This is mega dumb but it paves the way for custom keywords
public static class KeywordRegistry
{
    /// <summary> All currently registered keywords. </summary>
    public static IReadOnlyDictionary<string, Token.TokenType> Keywords => keywords;

    private static Dictionary<string, Token.TokenType> keywords = new()
    {
        { "module", Token.TokenType.Module },
        { "import", Token.TokenType.Import },
        { "unsafe", Token.TokenType.Unsafe },

        { "void", Token.TokenType.Void },
    };

    /// <summary> Registers a new keyword with its associated token type. </summary>
    public static void Register(string keyword, Token.TokenType type) => keywords[keyword] = type;

    /// <summary> Tries to get the token type for a keyword. Returns false if it's not a keyword. </summary>
    public static bool TryGetKeywordType(string value, out Token.TokenType type) => keywords.TryGetValue(value, out type);
}
