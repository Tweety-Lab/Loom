using MediatR;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Server.Capabilities;
using Loom.Integration.LanguageServer.Documents;

namespace Loom.Integration.LanguageServer.Handlers;

internal class TextDocumentSyncHandler(ParsedDocumentStore documents, DocumentDiagnosticService diagnostics) : TextDocumentSyncHandlerBase
{
    /// <inheritdoc/>
    public override TextDocumentAttributes GetTextDocumentAttributes(DocumentUri uri) => new(uri, "Loom");

    /// <inheritdoc/>
    public override async Task<Unit> Handle(DidOpenTextDocumentParams request, CancellationToken cancellationToken)
    {
        documents.Open(request.TextDocument);
        await diagnostics.PublishAsync(request.TextDocument.Uri, cancellationToken);
        return Unit.Value;
    }

    /// <inheritdoc/>
    public override Task<Unit> Handle(DidCloseTextDocumentParams request, CancellationToken cancellationToken)
    {
        documents.Close(request.TextDocument.Uri);
        diagnostics.Clear(request.TextDocument.Uri);
        return Unit.Task;
    }

    /// <inheritdoc/>
    public override async Task<Unit> Handle(DidChangeTextDocumentParams request, CancellationToken cancellationToken)
    {
        if (documents.Update(request.TextDocument.Uri, request.TextDocument.Version, request.ContentChanges))
            await diagnostics.PublishAsync(request.TextDocument.Uri, cancellationToken);
        return Unit.Value;
    }

    /// <inheritdoc/>
    public override Task<Unit> Handle(DidSaveTextDocumentParams request, CancellationToken cancellationToken) => Unit.Task;

    /// <inheritdoc/>
    protected override TextDocumentSyncRegistrationOptions CreateRegistrationOptions(TextSynchronizationCapability capability, ClientCapabilities clientCapabilities)
        => new()
        {
            DocumentSelector = new TextDocumentSelector(new TextDocumentFilter { Pattern = "**/*.loom" }),
            Change = TextDocumentSyncKind.Incremental,
            Save = new SaveOptions { IncludeText = false }
        };
}
