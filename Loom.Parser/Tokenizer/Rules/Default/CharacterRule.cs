
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
        var next = Reader.PeekChar();

        if (TokenRegistry.TryGetMultiCharacterType($"{c}{next}", out var multiCharType))
        {
            Reader.Read();
            return CreateToken(multiCharType, $"{c}{next}");
        }

        TokenRegistry.TryGetCharacterType(c, out var type);
        return CreateToken(type, c.ToString());
    }
}
