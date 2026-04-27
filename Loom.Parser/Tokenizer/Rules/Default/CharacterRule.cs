
namespace Loom.Parser.Tokenizer.Rules.Default;

[TokenizerRule]
public class CharacterRule : TokenizerRule
{
    /// <inheritdoc />
    public override bool CanHandle(char current) => TokenRegistry.Characters.ContainsKey(current);

    /// <inheritdoc />
    public override Token Read()
    {
        var c = (char)Reader.Read();
        TokenRegistry.TryGetCharacterType(c, out var type);
        return CreateToken(type, c.ToString());
    }
}
