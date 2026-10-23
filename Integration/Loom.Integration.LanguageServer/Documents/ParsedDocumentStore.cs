using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using Loom.Common.Diagnostics;
using Loom.Parser;
using Loom.Parser.AST.Rules.Default;
using Loom.Parser.Tokenizer;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace Loom.Integration.LanguageServer.Documents;

/// <summary>
/// Keeps track of open documents and their syntax.
/// </summary>
internal sealed class ParsedDocumentStore
{
    private readonly ConcurrentDictionary<DocumentUri, Entry> documents = new();

    /// <summary> Opens a new document. </summary>
    public void Open(TextDocumentItem document)
    {
        var snapshot = new TextDocumentSnapshot(document.Uri, document.LanguageId, document.Version, document.Text);
        documents[document.Uri] = new Entry(snapshot);
    }

    /// <summary> Attempts to get the <see cref="TextDocumentSnapshot"/> for an open document. </summary>
    public bool TryGet(DocumentUri uri, [NotNullWhen(true)] out TextDocumentSnapshot? document)
    {
        if (documents.TryGetValue(uri, out var entry))
        {
            document = entry.Document;
            return true;
        }

        document = null;
        return false;
    }

    /// <summary> Gets all open documents. </summary>
    public IReadOnlyList<TextDocumentSnapshot> GetOpenDocuments() => [.. documents.Values.Select(entry => entry.Document)];

    /// <summary> Applies an edit batch. </summary>
    public bool Update(DocumentUri uri, int? version, IEnumerable<TextDocumentContentChangeEvent> changes)
    {
        if (!documents.TryGetValue(uri, out var entry))
            return false;

        var document = entry.Document;
        if (version.HasValue && document.Version.HasValue && version.Value <= document.Version.Value)
            return false;

        var updated = document.WithChanges(version, changes);
        return documents.TryUpdate(uri, new Entry(updated), entry);
    }

    /// <summary> Gets the syntax for an open document. </summary>
    public async Task<ParsedDocument?> GetOrParseAsync(DocumentUri uri, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!documents.TryGetValue(uri, out var entry))
            return null;

        return await entry.Parse.Value.WaitAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary> Closes a document. </summary>
    public bool Close(DocumentUri uri) => documents.TryRemove(uri, out _);

    private sealed class Entry(TextDocumentSnapshot document)
    {
        public TextDocumentSnapshot Document { get; } = document;
        public Lazy<Task<ParsedDocument>> Parse { get; } = new(() => Task.Run(() => ParseDocument(document)));
    }

    private static ParsedDocument ParseDocument(TextDocumentSnapshot document)
    {
        var diagnostics = new DiagnosticContext();
        ProgramNode? root = null;
        try
        {
            var tokens = new LoomTokenizer(document.Text).Tokenize();
            root = new LoomParser(tokens, diagnostics).ParseProgram(document.Uri.ToString());
        }
        catch (ParseFailureException) { }

        return new ParsedDocument(document, root, [.. diagnostics.Diagnostics]);
    }
}