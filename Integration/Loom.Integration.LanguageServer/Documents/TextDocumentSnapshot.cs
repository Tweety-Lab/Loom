using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace Loom.Integration.LanguageServer.Documents;

/// <summary>
/// Snapshot of an open editor document.
/// </summary>
internal sealed class TextDocumentSnapshot(DocumentUri uri, string languageId, int? version, string text)
{
    private readonly Lazy<int[]> lineStarts = new(() => GetLineStarts(text));

    public DocumentUri Uri { get; } = uri;
    public string LanguageId { get; } = languageId;
    public int? Version { get; } = version;
    public string Text { get; } = text;

    public int GetOffset(Position position)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(position.Line);
        ArgumentOutOfRangeException.ThrowIfNegative(position.Character);

        var starts = lineStarts.Value;
        if (position.Line >= starts.Length)
            return Text.Length;

        var start = starts[position.Line];
        var end = position.Line + 1 < starts.Length ? starts[position.Line + 1] : Text.Length;

        if (end > start && Text[end - 1] == '\n')
            end--;

        if (end > start && Text[end - 1] == '\r')
            end--;

        return start + Math.Min(position.Character, end - start);
    }

    internal TextDocumentSnapshot WithChanges(int? newVersion, IEnumerable<TextDocumentContentChangeEvent> changes)
    {
        var updated = this;
        foreach (var change in changes)
        {
            string newText;
            if (change.Range is { } range)
            {
                var start = updated.GetOffset(range.Start);
                var end = updated.GetOffset(range.End);
                if (end < start)
                    throw new ArgumentException("A document change range must end at or after its start.", nameof(changes));

                newText = string.Concat(updated.Text.AsSpan(0, start), change.Text, updated.Text.AsSpan(end));
            }
            else
                newText = change.Text;

            updated = new TextDocumentSnapshot(Uri, LanguageId, newVersion ?? Version, newText);
        }

        return new TextDocumentSnapshot(Uri, LanguageId, newVersion ?? Version, updated.Text);
    }

    private static int[] GetLineStarts(string text)
    {
        var starts = new List<int> { 0 };
        for (var i = 0; i < text.Length; i++)
        {
            if (text[i] == '\r')
            {
                if (i + 1 < text.Length && text[i + 1] == '\n')
                    i++;

                starts.Add(i + 1);
            }
            else if (text[i] == '\n')
                starts.Add(i + 1);
        }

        return [.. starts];
    }
}