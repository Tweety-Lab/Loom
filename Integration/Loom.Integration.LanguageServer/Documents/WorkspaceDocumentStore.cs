using System.Collections.Concurrent;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;

namespace Loom.Integration.LanguageServer.Documents;

/// <summary>
/// Keeps track of documents that makeup the total workspace.
/// </summary>
internal sealed class WorkspaceDocumentStore(ParsedDocumentStore documents, ILanguageServerFacade server)
{
    // This is dumb
    public static readonly HashSet<string> IgnoredDirectories = new(StringComparer.OrdinalIgnoreCase)
    {
        ".git", ".vs", "bin", "obj"
    };

    private readonly ConcurrentDictionary<DocumentUri, Entry> files = new();

    public async Task<IReadOnlyList<ParsedDocument>> GetDocumentsAsync(DocumentUri currentUri, CancellationToken cancellationToken)
    {
        var result = new List<ParsedDocument>();
        var found = new HashSet<DocumentUri>();

        foreach (var root in GetRoots(currentUri).Distinct(StringComparer.OrdinalIgnoreCase))
            foreach (var path in EnumerateSources(root, cancellationToken))
            {
                cancellationToken.ThrowIfCancellationRequested();
                var uri = DocumentUri.FromFileSystemPath(path);
                if (!found.Add(uri) || uri == currentUri || documents.TryGet(uri, out _))
                    continue;

                try
                {
                    var text = await File.ReadAllTextAsync(path, cancellationToken);
                    var entry = files.AddOrUpdate(uri,
                        _ => new Entry(new TextDocumentSnapshot(uri, "Loom", null, text)),
                        (_, previous) => previous.Document.Text == text ? previous : new Entry(new TextDocumentSnapshot(uri, "Loom", null, text)));
                    result.Add(await entry.Parse.Value.WaitAsync(cancellationToken));
                }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
            }

        foreach (var uri in files.Keys)
            if (!found.Contains(uri))
                files.TryRemove(uri, out _);

        return result;
    }

    private IEnumerable<string> GetRoots(DocumentUri currentUri)
    {
        var settings = server.ClientSettings;
        var roots = settings.WorkspaceFolders?.Select(folder => folder.Uri).Where(uri => uri.Scheme == "file").Select(uri => uri.GetFileSystemPath()).ToArray() ?? [];

        if (roots.Length > 0)
            return roots;

        if (settings.RootUri is { Scheme: "file" } rootUri)
            return [rootUri.GetFileSystemPath()];

        if (!string.IsNullOrWhiteSpace(settings.RootPath))
            return [settings.RootPath];

        if (currentUri.Scheme != "file" || Path.GetDirectoryName(currentUri.GetFileSystemPath()) is not { } directory)
            return [];

        for (var candidate = new DirectoryInfo(directory); candidate is not null; candidate = candidate.Parent)
        {
            try
            {
                if (candidate.EnumerateFiles("*.lmproj").Any())
                    return [candidate.FullName];
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }

        return [directory];
    }

    private static IEnumerable<string> EnumerateSources(string root, CancellationToken cancellationToken)
    {
        var pending = new Stack<string>();
        pending.Push(root);

        while (pending.TryPop(out var directory))
        {
            cancellationToken.ThrowIfCancellationRequested();
            string[] sources;
            string[] children;

            try
            {
                sources = Directory.GetFiles(directory, "*.loom");
                children = Directory.GetDirectories(directory);
            }
            catch (IOException) { continue; }
            catch (UnauthorizedAccessException) { continue; }

            foreach (var source in sources)
                yield return source;

            foreach (var child in children)
            {
                if (IgnoredDirectories.Contains(Path.GetFileName(child)))
                    continue;

                try
                {
                    if ((File.GetAttributes(child) & FileAttributes.ReparsePoint) == 0)
                        pending.Push(child);
                }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
            }
        }
    }

    private sealed class Entry(TextDocumentSnapshot document)
    {
        public TextDocumentSnapshot Document { get; } = document;
        public Lazy<Task<ParsedDocument>> Parse { get; } = new(() => Task.Run(() => ParsedDocumentStore.ParseDocument(document)));
    }
}
