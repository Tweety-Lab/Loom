
namespace Loom.Parser.Tokenizer.Rules.Default;

[TokenizerRule]
public class CharacterRule : TokenizerRule
{
    // This is super hacky, we should add an attribute for these
    private static readonly Dictionary<(char, char), Token.TokenType> twoCharacterOperators = new()
    {
        [('=', '=')] = Token.TokenType.EqualEqual,
        [('!', '=')] = Token.TokenType.NotEqual,
        [('<', '=')] = Token.TokenType.LessEqual,
        [('>', '=')] = Token.TokenType.GreaterEqual,
    };

    /// <inheritdoc />
    public override bool CanHandle(char current) => TokenRegistry.Characters.ContainsKey(current);

    /// <inheritdoc />
    public override Token Read()
    {
        var c = (char)Reader.Read();
        var next = (char)Reader.PeekChar();

        if (twoCharacterOperators.TryGetValue((c, next), out var twoCharType))
        {
            Reader.Read();
            return CreateToken(twoCharType, $"{c}{next}");
        }

        TokenRegistry.TryGetCharacterType(c, out var type);
        return CreateToken(type, c.ToString());
    }
}
