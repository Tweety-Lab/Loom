using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace Loom.Integration.LanguageServer.Documents;

internal sealed class DocumentStore
{
    private readonly ConcurrentDictionary<DocumentUri, TextDocumentSnapshot> documents = new();

    /// <summary> Opens a new document. </summary>
    public void Open(TextDocumentItem document)
    {
        var snapshot = new TextDocumentSnapshot(document.Uri, document.LanguageId, document.Version, document.Text);
        documents[document.Uri] = snapshot;
    }

    /// <summary> Attempts to load a document from it's <see cref="DocumentUri"/>. </summary>
    public bool TryGet(DocumentUri uri, [NotNullWhen(true)] out TextDocumentSnapshot? document) => documents.TryGetValue(uri, out document);

    /// <summary> Gets all currently opened documents. </summary>
    public IReadOnlyList<TextDocumentSnapshot> GetOpenDocuments() => [.. documents.Values];

    /// <summary> Applies a whole change batch. </summary>
    public bool Update(DocumentUri uri, int? version, IEnumerable<TextDocumentContentChangeEvent> changes)
    {
        if (!documents.TryGetValue(uri, out var document))
            return false;

        if (version.HasValue && document.Version.HasValue && version.Value <= document.Version.Value)
            return false;

        var updated = document.WithChanges(version, changes);

        return documents.TryUpdate(uri, updated, document);
    }

    public bool Close(DocumentUri uri)
    {
        return documents.TryRemove(uri, out _);
    }
}