
namespace Loom.Parser.Tokenizer.Rules.Default;

[TokenizerRule]
public class CharacterRule : ITokenizerRule
{
    /// <inheritdoc />
    public bool CanHandle(char current) => TokenRegistry.Characters.ContainsKey(current);

    /// <inheritdoc />
    public Token Read(LoomStringReader reader)
    {
        var c = (char)reader.Read();
        TokenRegistry.TryGetCharacterType(c, out var type);
        return new Token(type, c.ToString());
    }
}
