using System.Reflection;

namespace Loom.Parser.Tokenizer;

public static class ModifierRegistry
{
    /// <summary> All currently registered modifiers. </summary>
    public static IEnumerable<Token.TokenType> Modifiers => modifiers;

    private static HashSet<Token.TokenType> modifiers;

    static ModifierRegistry()
    {
        modifiers = typeof(Token.TokenType).GetFields().Where(f => f.IsDefined(typeof(ModifierAttribute))).Select(f => (Token.TokenType)f.GetValue(null)!).ToHashSet();
    }

    /// <summary> Checks if a token type is a modifier. </summary>
    public static bool IsModifier(Token.TokenType type) => modifiers.Contains(type);
}
