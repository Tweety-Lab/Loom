using Loom.Analyzer;
using Loom.Parser.AST.Rules.Default;

namespace Loom.Integration.LanguageServer.Documents;

/// <summary>
/// Builds semantic analysis for <see cref="ParsedDocument"/>s.
/// </summary>
internal sealed class DocumentAnalysisService(WorkspaceDocumentStore workspace)
{
    public async Task<AnalysisContext> AnalyzeAsync(ParsedDocument document, CancellationToken cancellationToken = default) => (await AnalyzeWithDocumentsAsync(document, cancellationToken)).Analysis;

    public async Task<(AnalysisContext Analysis, IReadOnlyList<ParsedDocument> Documents)> AnalyzeWithDocumentsAsync(ParsedDocument document, CancellationToken cancellationToken = default)
    {
        var sources = new List<ParsedDocument> { document };

        foreach (var parsed in await workspace.GetDocumentsAsync(document.Document.Uri, cancellationToken))
            sources.Add(parsed);

        cancellationToken.ThrowIfCancellationRequested();
        var analysis = new AnalysisContext();
        analysis.Analyze(sources.Select(source => source.Root).OfType<ProgramNode>());
        cancellationToken.ThrowIfCancellationRequested();
        return (analysis, sources);
    }
}