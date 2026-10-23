using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Loom.Integration.LanguageServer.Presentation;
using Loom.Integration.LanguageServer.Presentation.Generic;
using Loom.Integration.LanguageServer.Documents;
using Loom.Analyzer.Symbols;
using System.Diagnostics.CodeAnalysis;

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

        if (symbol is null)
            return null;

        if (!TryBuildSymbolName(symbol, out var builder))
            return null;

        return builder.BuildHover();
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

    private bool TryBuildSymbolName(Symbol symbol, [NotNullWhen(true)] out ClassifiedTextBuilder? builder)
    {
        switch (symbol)
        {
            case TypeSymbol type:
                builder = BuildTypeSymbolName(type);
                return true;
            default:
                builder = null;
                return false;
        }
    }

    private ClassifiedTextBuilder BuildTypeSymbolName(TypeSymbol symbol)
    {
        ClassifiedTextBuilder builder = new ClassifiedTextBuilder(backend);

        builder.Keyword(symbol.IsExported ? "export " : "");
        builder.Keyword(symbol.IsStatic ? "static " : "");
        builder.Keyword(symbol.IsStackOnly ? "stack " : "");

        ReadOnlySpan<string> parts = symbol.FullyQualifiedName.Split("::");

        builder.Keyword(symbol.IsValueType ? "struct " : "class ");

        for (int i = 0; i < parts.Length - 1; i++)
        {
            builder.Namespace(parts[i]);
            builder.Punctuation("::");
        }

        if (symbol.IsValueType)
            builder.StructName(parts[^1]);
        else
            builder.ClassName(parts[^1]);

        return builder;
    }
}
