using Loom.Integration.LanguageServer.Documents;
using MediatR;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Workspace;

using FileSystemWatcher = OmniSharp.Extensions.LanguageServer.Protocol.Models.FileSystemWatcher;

namespace Loom.Integration.LanguageServer.Handlers;

internal sealed class WorkspaceFilesHandler(DocumentDiagnosticService diagnostics) : DidChangeWatchedFilesHandlerBase
{
    /// <inheritdoc/>
    public override async Task<Unit> Handle(DidChangeWatchedFilesParams request, CancellationToken cancellationToken)
    {
        await diagnostics.PublishOpenDocumentsAsync(cancellationToken);
        return Unit.Value;
    }

    /// <inheritdoc/>
    protected override DidChangeWatchedFilesRegistrationOptions CreateRegistrationOptions(DidChangeWatchedFilesCapability capability, ClientCapabilities clientCapabilities) => new()
    {
        Watchers = new Container<FileSystemWatcher>(new FileSystemWatcher { GlobPattern = new GlobPattern("**/*.loom") })
    };
}
