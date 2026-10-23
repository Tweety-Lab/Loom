using Loom.Common.Diagnostics;
using Loom.Parser.Tokenizer;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;

using LspDiagnostic = OmniSharp.Extensions.LanguageServer.Protocol.Models.Diagnostic;
using LoomDiagnostic = Loom.Common.Diagnostics.Diagnostic;
using LspRange = OmniSharp.Extensions.LanguageServer.Protocol.Models.Range;

namespace Loom.Integration.LanguageServer.Documents;

internal sealed class DocumentDiagnosticService(ParsedDocumentStore documents, DocumentAnalysisService analyses, ILanguageServerFacade server)
{
    public async Task PublishAsync(DocumentUri uri, CancellationToken cancellationToken)
    {
        var parsed = await documents.GetOrParseAsync(uri, cancellationToken);
        if (parsed is null)
            return;

        var diagnostics = parsed.Diagnostics.ToList();
        if (parsed.Root is { } root && diagnostics.Count == 0)
        {
            var analysis = await analyses.AnalyzeAsync(parsed, cancellationToken);
            var context = new DiagnosticContext();
            analysis.DiagnosticContext = context;

            foreach (var analyzer in analysis.Analyzers)
            {
                cancellationToken.ThrowIfCancellationRequested();
                root.Accept(analyzer);
            }

            diagnostics.AddRange(context.Diagnostics);
        }

        cancellationToken.ThrowIfCancellationRequested();
        if (!documents.TryGet(uri, out var current) || !ReferenceEquals(current, parsed.Document))
            return;

        var tokens = new LoomTokenizer(parsed.Document.Text).Tokenize();
        server.TextDocument.PublishDiagnostics(new PublishDiagnosticsParams
        {
            Uri = uri,
            Version = parsed.Document.Version,
            Diagnostics = new Container<LspDiagnostic>(diagnostics.Distinct().Select(diagnostic => new LspDiagnostic
            {
                Message = diagnostic.Message,
                Source = "Loom",
                Severity = diagnostic.Level switch
                {
                    LoomDiagnostic.DiagnosticLevel.Error => DiagnosticSeverity.Error,
                    LoomDiagnostic.DiagnosticLevel.Warning => DiagnosticSeverity.Warning,
                    _ => DiagnosticSeverity.Information
                },
                Range = GetRange(parsed.Document, tokens, diagnostic)
            }))
        });
    }

    public void Clear(DocumentUri uri) => server.TextDocument.PublishDiagnostics(new PublishDiagnosticsParams
    {
        Uri = uri,
        Diagnostics = new Container<LspDiagnostic>()
    });

    private static LspRange GetRange(TextDocumentSnapshot document, List<Token> tokens, LoomDiagnostic diagnostic)
    {
        if (diagnostic.Position is not { } position)
            return new LspRange(new Position(0, 0), new Position(0, 0));

        foreach (var token in tokens)
            if (token.Location.Line == position.Line && token.Location.Column == position.Column)
                return new LspRange(document.GetPosition(token.Start), document.GetPosition(token.End));

        var line = Math.Max(0, position.Line);
        var column = Math.Max(0, position.Column - (line > 0 ? 1 : 0));
        var offset = document.GetOffset(new Position(line, column));
        var point = document.GetPosition(offset);

        return new LspRange(point, point);
    }
}