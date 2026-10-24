using Loom.Analyzer;
using Loom.Analyzer.Symbols;
using Loom.Integration.LanguageServer.Documents;
using Loom.Parser.AST;
using Loom.Parser.AST.Rules.Default;
using Loom.Parser.Tokenizer;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace Loom.Integration.LanguageServer.Handlers;

internal sealed class DefinitionHandler(ParsedDocumentStore documents, DocumentAnalysisService analyses) : DefinitionHandlerBase
{
    /// <inheritdoc/>
    public override async Task<LocationOrLocationLinks?> Handle(DefinitionParams request, CancellationToken cancellationToken)
    {
        var parsed = await documents.GetOrParseAsync(request.TextDocument.Uri, cancellationToken);
        var offset = parsed?.Document.GetOffset(request.Position);

        if (offset is null || parsed?.Root?.FindNode(offset.Value) is not { } node)
            return null;

        var name = node switch
        {
            TypeNode type => type.Base,
            IdentifierNameNode identifier => identifier.Token,
            ITypeDeclarationNode declaration => declaration.Name,
            TypeParameterNode parameter => parameter.Token,
            MethodDeclarationNode method => method.Name,
            PropertyDeclarationNode property => property.Name,
            _ => null
        };

        if (name is null || offset < name.Start || offset >= name.End)
            return null;

        var (analysis, sources) = await analyses.AnalyzeWithDocumentsAsync(parsed, cancellationToken);
        var location = analysis.GetSymbol(node).Symbol switch
        {
            TypeSymbol type => LocationOfType(type, analysis, sources),
            MethodSymbol method => LocationOfMethod(method, analysis, sources),
            PropertySymbol property => LocationOfProperty(property, analysis, sources),
            _ => null
        };

        return location is null ? null : new LocationOrLocationLinks(location);
    }

    /// <inheritdoc/>
    protected override DefinitionRegistrationOptions CreateRegistrationOptions(DefinitionCapability capability, ClientCapabilities clientCapabilities) => new()
        {
            DocumentSelector = new TextDocumentSelector(new TextDocumentFilter { Pattern = "**/*.loom" })
        };

    public Location? LocationOfType(TypeSymbol type, AnalysisContext analysis, IReadOnlyList<ParsedDocument> sources)
    {
        while (type is ArrayTypeSymbol array)
            type = array.ElementType;

        var declaringNode = type.OriginalDefinition.DeclaringNode;
        var targetName = declaringNode switch
        {
            ITypeDeclarationNode declaration => declaration.Name,
            TypeParameterNode parameter => parameter.Token,
            _ => null
        };

        return LocationOf(declaringNode, targetName, analysis, sources);
    }

    public Location? LocationOfMethod(MethodSymbol method, AnalysisContext analysis, IReadOnlyList<ParsedDocument> sources)
    {
        var declaringNode = method.OriginalDefinition.DeclaringNode;
        var targetName = declaringNode switch
        {
            MethodDeclarationNode declaration => declaration.Name,
            ConstructorDeclarationNode constructor => constructor.Name,
            DestructorDeclarationNode destructor => destructor.Name,
            OperatorDeclarationNode op => op.Operator,
            _ => null
        };

        return LocationOf(declaringNode, targetName, analysis, sources);
    }

    public Location? LocationOfProperty(PropertySymbol property, AnalysisContext analysis, IReadOnlyList<ParsedDocument> sources)
    {
        var declaringNode = property.OriginalDefinition.DeclaringNode;
        var targetName = declaringNode switch
        {
            PropertyDeclarationNode declaration => declaration.Name,
            _ => null
        };

        return LocationOf(declaringNode, targetName, analysis, sources);
    }

    private static Location? LocationOf(ASTNode? declaringNode, Token? targetName, AnalysisContext analysis, IReadOnlyList<ParsedDocument> sources)
    {
        if (declaringNode is null || targetName is null)
            return null;

        var root = analysis.FirstAncestorOrSelf<ProgramNode>(declaringNode);
        var target = sources.FirstOrDefault(source => ReferenceEquals(source.Root, root));
        if (target is null)
            return null;

        return new Location
        {
            Uri = target.Document.Uri,
            Range = new OmniSharp.Extensions.LanguageServer.Protocol.Models.Range(target.Document.GetPosition(targetName.Start), target.Document.GetPosition(targetName.End))
        };
    }
}
