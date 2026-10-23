using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace Loom.Integration.LanguageServer.Handlers;

internal class HoverHandler : HoverHandlerBase
{
    /// <inheritdoc/>
    public override Task<Hover?> Handle(HoverParams request, CancellationToken cancellationToken)
    {
        var markup = new MarkupContent()
        {
            Kind = MarkupKind.Markdown,
            Value = "**Loom** Hover Text!"
        };

        var contents = new MarkedStringsOrMarkupContent(markup);

        return Task.FromResult<Hover?>(new Hover { Contents = contents });
    }

    /// <inheritdoc/>
    protected override HoverRegistrationOptions CreateRegistrationOptions(HoverCapability capability, ClientCapabilities clientCapabilities)
    {
        return new HoverRegistrationOptions
        {
            DocumentSelector = new TextDocumentSelector(new TextDocumentFilter { Pattern = "**/*.loom" })
        };
    }
}
