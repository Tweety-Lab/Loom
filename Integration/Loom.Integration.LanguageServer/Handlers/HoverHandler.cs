using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Loom.Integration.LanguageServer.Presentation;
using Loom.Integration.LanguageServer.Presentation.Generic;
using Loom.Integration.LanguageServer.Documents;

namespace Loom.Integration.LanguageServer.Handlers;

internal class HoverHandler(ParsedDocumentStore documents, DocumentAnalysisService analyses) : HoverHandlerBase
{
    private IClassifiedTextBackend backend = new GenericClassifiedTextBackend();

    /// <inheritdoc/>
    public override async Task<Hover?> Handle(HoverParams request, CancellationToken cancellationToken)
    {
        var parsed = await documents.GetOrParseAsync(request.TextDocument.Uri, cancellationToken);
        if (parsed?.Root is null)
            return null;

        var node = parsed.Root.FindNode(parsed.Document.GetOffset(request.Position));
        if (node is null)
            return null;

        var analysis = await analyses.AnalyzeAsync(parsed, cancellationToken);
        var symbol = analysis.GetSymbol(node).Symbol;
        return symbol is null ? null : new ClassifiedTextBuilder(backend).StructName(symbol.FullyQualifiedName).BuildHover();
    }

    /// <inheritdoc/>
    protected override HoverRegistrationOptions CreateRegistrationOptions(HoverCapability capability, ClientCapabilities clientCapabilities)
    {
        backend = IClassifiedTextBackend.ForClient(clientCapabilities);
        return new HoverRegistrationOptions
        {
            DocumentSelector = new TextDocumentSelector(new TextDocumentFilter { Pattern = "**/*.loom" })
        };
    }
}
