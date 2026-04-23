using Loom.Common.Exceptions;
using Loom.Parser.Tokenizer;

using static Loom.Parser.Tokenizer.Token;

namespace Loom.Parser;

public class TokenReader
{
    /// <summary> All tokens that make up the source. </summary>
    public List<Token> Tokens { get; }

    /// <summary> The current position in the token list. </summary>
    public int Position { get; private set; }

    /// <summary> The current token. </summary>
    public Token Current => Tokens[Position];

    /// <summary> Initializes a new instance of the <see cref="TokenReader"/> class. </summary>
    public TokenReader(List<Token> tokens) => Tokens = tokens;

    public Token Peek(int offset = 1)
    {
        var index = Position + offset;
        return index < Tokens.Count ? Tokens[index] : Tokens[^1];
    }

    public Token Advance()
    {
        var token = Current;
        Position++;
        return token;
    }

    public Token Expect(TokenType type)
    {
        if (Current.Type != type)
            throw new LoomException($"Expected {type}, got {Current.Type}");

        return Advance();
    }

    public bool Match(TokenType type)
    {
        if (Current.Type == type)
        {
            Advance();
            return true;
        }

        return false;
    }

    public bool Check(TokenType type) => Current.Type == type;
}
