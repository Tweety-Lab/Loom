
namespace Loom.Parser.Tokenizer;

/// <summary>
/// A Loom string reader.
/// </summary>
public class LoomStringReader
{
    /// <summary> The source string. </summary>
    public string Source { get; init; }
    
    /// <summary> The current reading position in the source string. </summary>
    public int Position { get; private set; }

    /// <summary> The current reading line in the source string. </summary>
    public int Line { get; private set; } = 1;

    /// <summary> The current reading column in the source string. </summary>
    public int Column { get; private set; } = 1;

    /// <summary> Checks if the reader is at the end of the source string. </summary>
    public bool IsEnd => Position >= Source.Length;

    /// <inheritdoc/>
    public LoomStringReader(string s) => Source = s;

    /// <summary> Peeks at the next character in the source string. </summary>
    public int Peek(int offset = 0) => Position + offset < Source.Length ? Source[Position + offset] : -1;

    /// <summary> Peeks at the next character in the source string. </summary>
    public char PeekChar(int offset = 0) => (char)(Peek(offset) == -1 ? '\0' : Peek(offset));

    /// <summary> Reads the next character in the source string. </summary>
    public int Read()
    {
        if (Peek() == -1)
            return -1;

        var c = Source[Position++];

        if (c == '\n')
        {
            Line++;
            Column = 1;
        }
        else
        {
            Column++;
        }

        return c;
    }
}
