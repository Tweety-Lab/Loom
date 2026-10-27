using OmniSharp.Extensions.LanguageServer.Protocol.Models;

using LspRange = OmniSharp.Extensions.LanguageServer.Protocol.Models.Range;

namespace Loom.Integration.LanguageServer.Presentation.Generic;

/// <summary>
/// Basic implementation of <see cref="IClassifiedTextBackend"/> that uses plain text.
/// </summary>
internal sealed class GenericClassifiedTextBackend : IClassifiedTextBackend
{
    public CompletionItem CreateCompletionItem(ClassifiedText? signature, ClassifiedText? documentation) => new()
    {
        Detail = signature?.Text,
        Documentation = documentation is null ? null : new StringOrMarkupContent(new MarkupContent
        {
            Kind = MarkupKind.PlainText,
            Value = documentation.Text
        })
    };

    public Hover CreateHover(ClassifiedText text, LspRange? range = null) => new()
    {
        Contents = new MarkedStringsOrMarkupContent(new MarkupContent
        {
            Kind = MarkupKind.PlainText,
            Value = text.Text
        }),
        Range = range
    };
}