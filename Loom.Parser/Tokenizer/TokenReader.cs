
using Loom.Common.Diagnostics;
using static Loom.Parser.Tokenizer.Token;

namespace Loom.Parser.Tokenizer;

public class TokenReader
{
    /// <summary> The <see cref="Common.Diagnostics.DiagnosticContext"/> this reports to, if any. </summary>
    public DiagnosticContext? DiagnosticContext { get; set; }

    /// <summary> All tokens that make up the source. </summary>
    public List<Token> Tokens { get; }

    /// <summary> The current position in the token list. </summary>
    public int Position { get; private set; }

    /// <summary> The current token. </summary>
    public Token Current => Tokens[Position];

    /// <summary> Initializes a new instance of the <see cref="TokenReader"/> class. </summary>
    public TokenReader(List<Token> tokens, DiagnosticContext? diagnosticContext = null)
    {
        Tokens = tokens;
        DiagnosticContext = diagnosticContext;
    }

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
            DiagnosticContext?.Report(new Diagnostic(Diagnostic.DiagnosticLevel.Error, $"Expected {type}, got {Current.Type}"));

        return Advance();
    }

    public Token ExpectAny(params TokenType[] types)
    {
        if (!types.Contains(Current.Type))
            DiagnosticContext?.Report(new Diagnostic(Diagnostic.DiagnosticLevel.Error, $"Expected {string.Join(" or ", types)}, got {Current.Type}"));

        return Advance();
    }

    public List<Token> ExpectMany(Func<Token, bool> predicate)
    {
        var result = new List<Token>();

        while (predicate(Current))
            result.Add(Advance());

        return result;
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
