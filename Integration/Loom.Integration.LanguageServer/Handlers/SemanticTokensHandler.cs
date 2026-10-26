using Loom.Analyzer;
using Loom.Analyzer.Symbols;
using Loom.Integration.LanguageServer.Documents;
using Loom.Parser.AST;
using Loom.Parser.AST.Rules.Default;
using Loom.Parser.Tokenizer;
using Newtonsoft.Json.Linq;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace Loom.Integration.LanguageServer.Handlers;

internal sealed class SemanticTokensHandler(ParsedDocumentStore documents, DocumentAnalysisService analyses) : SemanticTokensHandlerBase
{
    private Dictionary<SemanticTokenType, SemanticTokenType> tokenTypes = CreateTokenTypes(false, []);
    private SemanticTokensLegend legend = CreateLegend(CreateTokenTypes(false, []).Values);

    /// <inheritdoc/>
    protected override Task<SemanticTokensDocument> GetSemanticTokensDocument(ITextDocumentIdentifierParams @params, CancellationToken cancellationToken) => Task.FromResult(new SemanticTokensDocument(legend));

    /// <inheritdoc/>
    protected override async Task Tokenize(SemanticTokensBuilder builder, ITextDocumentIdentifierParams identifier, CancellationToken cancellationToken)
    {
        var parsed = await documents.GetOrParseAsync(identifier.TextDocument.Uri, cancellationToken);
        if (parsed?.Root is null)
            return;

        var analysis = await analyses.AnalyzeAsync(parsed, cancellationToken);
        foreach (var (name, type) in Classify(parsed.Root, analysis, cancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var position = parsed.Document.GetPosition(name.Start);
            builder.Push(position.Line, position.Character, name.End - name.Start, tokenTypes[type],
                type == SemanticTokenType.Comment ? new[] { SemanticTokenModifier.Documentation } : Array.Empty<SemanticTokenModifier>());
        }
    }

    /// <inheritdoc/>
    protected override SemanticTokensRegistrationOptions CreateRegistrationOptions(SemanticTokensCapability capability, ClientCapabilities clientCapabilities)
    {
        var isVisualStudio = clientCapabilities.ExtensionData?.TryGetValue("_vs_supportsVisualStudioExtensions", out var supported) == true && supported.Type == JTokenType.Boolean && (bool)supported;
        tokenTypes = CreateTokenTypes(isVisualStudio, capability.TokenTypes);
        legend = CreateLegend(tokenTypes.Values);

        return new SemanticTokensRegistrationOptions
        {
            DocumentSelector = new TextDocumentSelector(new TextDocumentFilter { Pattern = "**/*.loom" }),
            Legend = legend,
            Full = true
        };
    }

    private static SemanticTokensLegend CreateLegend(IEnumerable<SemanticTokenType> types) => new()
    {
        TokenTypes = new(types),
        TokenModifiers = new(SemanticTokenModifier.Documentation)
    };

    public static Dictionary<SemanticTokenType, SemanticTokenType> CreateTokenTypes(bool isVisualStudio, IEnumerable<SemanticTokenType> supportedTypes)
    {
        var supported = supportedTypes.ToHashSet();
        return new[]
        {
            (Type: SemanticTokenType.Struct, VisualStudioName: "struct name"),
            (Type: SemanticTokenType.Method, VisualStudioName: "method name"),
            (Type: SemanticTokenType.TypeParameter, VisualStudioName: "type parameter name"),
            (Type: SemanticTokenType.Parameter, VisualStudioName: "parameter name"),
            (Type: SemanticTokenType.Variable, VisualStudioName: "local name"),
            (Type: SemanticTokenType.Comment, VisualStudioName: "xml doc comment - text"),
        }.ToDictionary(entry => entry.Type, entry =>
        {
            var visualStudioType = new SemanticTokenType(entry.VisualStudioName);
            return isVisualStudio || supported.Contains(visualStudioType) ? visualStudioType : entry.Type;
        });
    }

    public static IEnumerable<(Token Name, SemanticTokenType Type)> Classify(ASTNode root, AnalysisContext analysis, CancellationToken cancellationToken = default)
    {
        var tokens = new List<(Token Name, SemanticTokenType Type)>();
        foreach (var node in root.Descendants.Prepend(root))
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (node is IDocumentableNode documentable)
                foreach (var comment in documentable.DocumentComments)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (comment.Span.StartToken is { } token && token.End > token.Start)
                        tokens.Add((token, SemanticTokenType.Comment));
                }

            if (GetName(node) is not { } name || name.End <= name.Start)
                continue;

            if (GetTokenType(analysis.GetSymbol(node)) is { } type)
                tokens.Add((name, type));
        }

        return tokens.DistinctBy(token => token.Name.Start).OrderBy(token => token.Name.Start);
    }

    private static Token? GetName(ASTNode node) => node switch
    {
        ITypeDeclarationNode declaration => declaration.Name,
        MethodDeclarationNode declaration => declaration.Name,
        ParameterNode parameter => parameter.Name,
        TypeParameterNode parameter => parameter.Token,
        TypeNode type => type.Base,
        LocalDeclarationStatementNode declaration => declaration.Variable.Name,
        IdentifierNameNode identifier => identifier.Token,
        _ => null
    };

    private static SemanticTokenType? GetTokenType(SymbolInfo info)
    {
        var symbol = info.Symbol;
        while (symbol is ArrayTypeSymbol array)
            symbol = array.ElementType;

        return symbol switch
        {
            TypeSymbol { KnownType: TypeSymbol.DefaultType.Struct } => SemanticTokenType.Struct,
            TypeSymbol { KnownType: TypeSymbol.DefaultType.TypeParameter } => SemanticTokenType.TypeParameter,
            MethodSymbol { Kind: MethodSymbol.MethodKind.Method } => SemanticTokenType.Method,
            ParameterSymbol => SemanticTokenType.Parameter,
            LocalVariableSymbol => SemanticTokenType.Variable,
            null when info.Candidates.Count > 0 && info.Candidates.All(candidate => candidate is MethodSymbol { Kind: MethodSymbol.MethodKind.Method }) => SemanticTokenType.Method,
            _ => (SemanticTokenType?)null
        };
    }
}
