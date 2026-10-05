using Loom.Parser.Tokenizer;

namespace Loom.Parser;

/// <summary>
/// Thrown when a <see cref="TokenReader"/> read that had to succeed did not.
/// </summary>
public sealed class ParseFailureException : Exception
{
    /// <summary> The <see cref="Tokenizer.Token"/> the reader was positioned at when the read failed. </summary>
    public Token Token { get; }

    public ParseFailureException(Token token, string message) : base(message) => Token = token;
}