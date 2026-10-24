using Loom.Analyzer;
using Loom.Parser.AST.Rules.Default;

namespace Loom.Integration.LanguageServer.Documents;

/// <summary>
/// Builds semantic analysis for <see cref="ParsedDocument"/>s.
/// </summary>
internal sealed class DocumentAnalysisService(ParsedDocumentStore documents, WorkspaceDocumentStore workspace)
{
    public async Task<AnalysisContext> AnalyzeAsync(ParsedDocument document, CancellationToken cancellationToken = default) => (await AnalyzeWithDocumentsAsync(document, cancellationToken)).Analysis;

    public async Task<(AnalysisContext Analysis, IReadOnlyList<ParsedDocument> Documents)> AnalyzeWithDocumentsAsync(ParsedDocument document, CancellationToken cancellationToken = default)
    {
        var sources = new List<ParsedDocument> { document };

        foreach (var snapshot in documents.GetOpenDocuments())
        {
            if (snapshot.Uri == document.Document.Uri)
                continue;

            var parsed = await documents.GetOrParseAsync(snapshot.Uri, cancellationToken);
            if (parsed is not null)
                sources.Add(parsed);
        }

        foreach (var parsed in await workspace.GetDocumentsAsync(document.Document.Uri, cancellationToken))
            sources.Add(parsed);

        cancellationToken.ThrowIfCancellationRequested();
        var analysis = new AnalysisContext();
        analysis.Analyze(sources.Select(source => source.Root).OfType<ProgramNode>());
        cancellationToken.ThrowIfCancellationRequested();
        return (analysis, sources);
    }
}
