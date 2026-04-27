using System.Text;

namespace Loom.Parser.Tokenizer.Rules.Default;

[TokenizerRule]
public class NumberRule : TokenizerRule
{
    /// <inheritdoc />
    public override bool CanHandle(char current) => char.IsDigit(current);

    /// <inheritdoc />
    public override Token Read()
    {
        var sb = new StringBuilder();

        while (Reader.Peek() != -1 && char.IsDigit(Reader.PeekChar()))
            sb.Append((char)Reader.Read());

        return CreateToken(Token.TokenType.Number, sb.ToString());
    }
}

