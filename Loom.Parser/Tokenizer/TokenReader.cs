
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
    public int Position { get; set; }

    /// <summary> The current token. </summary>
    public Token Current => Tokens[Math.Min(Position, Tokens.Count - 1)];

    /// <summary> Initializes a new instance of the <see cref="TokenReader"/> class. </summary>
    public TokenReader(List<Token> tokens, DiagnosticContext? diagnosticContext = null)
    {
        Tokens = tokens;
        DiagnosticContext = diagnosticContext;
    }

    /// <summary> Reads an identifier optionally qualified with <c>::</c> segments (e.g. <c>Standard::Windows</c>). </summary>
    public Token ExpectQualifiedName()
    {
        var first = Expect(TokenType.Identifier);
        var segments = new List<string> { first.Text };

        while (Match(TokenType.ColonColon))
            segments.Add(Expect(TokenType.Identifier).Text);

        return new Token(TokenType.Identifier, string.Join("::", segments), first.Location);
    }

    public Token Peek(int offset = 1)
    {
        var index = Position + offset;
        return index < Tokens.Count ? Tokens[index] : Tokens[^1];
    }

    /// <summary> Advances to the next token. </summary>
    /// <returns> The consumed <see cref="Token"/>. </returns>
    public Token Advance()
    {
        var token = Current;

        if (Position < Tokens.Count - 1)
            Position++;

        return token;
    }

    /// <summary> Consumes the current <see cref="Token"/>, which must match the specified type. Reports an error and throws <see cref="ParseFailureException"/> otherwise. </summary>
    /// <returns> The consumed <see cref="Token"/>. </returns>
    /// <exception cref="ParseFailureException"> <paramref name="type"/> did not match the current token. </exception>
    public Token Expect(TokenType type)
    {
        if (Current.Type != type)
            Fail($"Expected {type}, got {Current.Type}");

        return Advance();
    }

    /// <summary> Consumes the current <see cref="Token"/>, which must satisfy the specified predicate. Reports an error and throws <see cref="ParseFailureException"/> otherwise. </summary>
    /// <returns> The consumed <see cref="Token"/>. </returns>
    /// <exception cref="ParseFailureException"> <paramref name="predicate"/> rejected the current token. </exception>
    public Token ExpectAny(Func<Token, bool> predicate)
    {
        if (!predicate(Current))
            Fail($"Expected {string.Join(" or ", predicate)}, got {Current.Type}");

        return Advance();
    }

    /// <summary> Reports <paramref name="message"/> at the current token, then aborts the calling rule. </summary>
    private void Fail(string message)
    {
        DiagnosticContext?.Report(new Diagnostic(Diagnostic.DiagnosticLevel.Error, message), Current.Location);
        throw new ParseFailureException(Current, message);
    }

    public List<Token> ExpectMany(Func<Token, bool> predicate)
    {
        var result = new List<Token>();

        while (predicate(Current))
            result.Add(Advance());

        return result;
    }

    /// <summary> Consumes the current token only if it matches the specified type. </summary>
    /// <returns> <see langword="true"/> if the token matched; otherwise, <see langword="false"/>. </returns>
    public bool Match(TokenType type)
    {
        if (Current.Type == type)
        {
            Advance();
            return true;
        }

        return false;
    }

    /// <summary> Checks if the current token matches the specified type without consuming. </summary>
    public bool Check(TokenType type) => Current.Type == type;
}
