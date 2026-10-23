using Loom.Analyzer;
using Loom.Parser.AST.Rules.Default;

namespace Loom.Integration.LanguageServer.Documents;

/// <summary>
/// Builds semantic analysis for <see cref="ParsedDocument"/>s.
/// </summary>
internal sealed class DocumentAnalysisService(ParsedDocumentStore documents)
{
    public async Task<AnalysisContext> AnalyzeAsync(ParsedDocument document, CancellationToken cancellationToken = default)
    {
        var roots = new List<ProgramNode>();
        if (document.Root is { } current)
            roots.Add(current);

        foreach (var snapshot in documents.GetOpenDocuments())
        {
            if (snapshot.Uri == document.Document.Uri)
                continue;

            var parsed = await documents.GetOrParseAsync(snapshot.Uri, cancellationToken);
            if (parsed?.Root is { } root)
                roots.Add(root);
        }

        cancellationToken.ThrowIfCancellationRequested();
        var analysis = new AnalysisContext();
        analysis.Analyze(roots);
        cancellationToken.ThrowIfCancellationRequested();
        return analysis;
    }
}