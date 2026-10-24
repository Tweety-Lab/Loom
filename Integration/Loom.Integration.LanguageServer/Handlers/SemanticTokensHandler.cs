using Loom.Integration.LanguageServer.Documents;
using Loom.Analyzer.Symbols;
using Loom.Parser.AST;
using Loom.Parser.AST.Rules.Default;
using Newtonsoft.Json.Linq;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace Loom.Integration.LanguageServer.Handlers;

internal sealed class SemanticTokensHandler(ParsedDocumentStore documents, DocumentAnalysisService analyses) : SemanticTokensHandlerBase
{
    private SemanticTokenType structTokenType = SemanticTokenType.Struct;
    private SemanticTokensLegend legend = new()
    {
        TokenTypes = new(SemanticTokenType.Struct),
        TokenModifiers = new()
    };

    /// <inheritdoc/>
    protected override Task<SemanticTokensDocument> GetSemanticTokensDocument(ITextDocumentIdentifierParams @params, CancellationToken cancellationToken) => Task.FromResult(new SemanticTokensDocument(legend));

    /// <inheritdoc/>
    protected override async Task Tokenize(SemanticTokensBuilder builder, ITextDocumentIdentifierParams identifier, CancellationToken cancellationToken)
    {
        var parsed = await documents.GetOrParseAsync(identifier.TextDocument.Uri, cancellationToken);
        if (parsed?.Root is null)
            return;

        var analysis = await analyses.AnalyzeAsync(parsed, cancellationToken);
        var names = parsed.Root.Descendants.Select(node =>
        {
            var type = analysis.GetSymbol(node).Symbol as TypeSymbol;
            while (type is ArrayTypeSymbol array)
                type = array.ElementType;

            if (type?.KnownType != TypeSymbol.DefaultType.Struct)
                return null;

            return node switch
            {
                ITypeDeclarationNode declaration => declaration.Name,
                TypeNode syntax => syntax.Base,
                IdentifierNameNode identifierName => identifierName.Token,
                _ => null
            };
        });

        foreach (var name in names.OfType<Loom.Parser.Tokenizer.Token>().DistinctBy(token => token.Start).OrderBy(token => token.Start))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var position = parsed.Document.GetPosition(name.Start);
            builder.Push(position.Line, position.Character, name.End - name.Start, structTokenType, Array.Empty<SemanticTokenModifier>());
        }
    }

    /// <inheritdoc/>
    protected override SemanticTokensRegistrationOptions CreateRegistrationOptions(SemanticTokensCapability capability, ClientCapabilities clientCapabilities)
    {
        var visualStudioStruct = new SemanticTokenType("struct name");
        var isVisualStudio = clientCapabilities.ExtensionData?.TryGetValue("_vs_supportsVisualStudioExtensions", out var supported) == true && supported.Type == JTokenType.Boolean && (bool)supported;
        structTokenType = isVisualStudio || capability.TokenTypes.Contains(visualStudioStruct) ? visualStudioStruct : SemanticTokenType.Struct;
        legend = new SemanticTokensLegend
        {
            TokenTypes = new(structTokenType),
            TokenModifiers = new()
        };

        return new SemanticTokensRegistrationOptions
        {
            DocumentSelector = new TextDocumentSelector(new TextDocumentFilter { Pattern = "**/*.loom" }),
            Legend = legend,
            Full = true
        };
    }
}
