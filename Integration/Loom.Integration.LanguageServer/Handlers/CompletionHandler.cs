using Loom.Integration.LanguageServer.Documents;
using Loom.Analyzer.Symbols;
using Loom.Parser.AST;
using Loom.Parser.AST.Rules.Default;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace Loom.Integration.LanguageServer.Handlers;

internal sealed class CompletionHandler(ParsedDocumentStore documents, DocumentAnalysisService analyses) : CompletionHandlerBase
{
    /// <inheritdoc/>
    public override Task<CompletionItem> Handle(CompletionItem request, CancellationToken cancellationToken)
    {
        return Task.FromResult(request);
    }

    /// <inheritdoc/>
    public override async Task<CompletionList> Handle(CompletionParams request, CancellationToken cancellationToken)
    {
        if (!documents.TryGet(request.TextDocument.Uri, out var document))
            return new CompletionList();

        var offset = document.GetOffset(request.Position);
        var start = offset;
        while (start > 0 && IsIdentifierCharacter(document.Text[start - 1]))
            start--;

        var dot = start - 1;
        while (dot >= 0 && char.IsWhiteSpace(document.Text[dot]))
            dot--;

        if (dot < 0 || document.Text[dot] != '.')
            return new CompletionList();

        var end = offset;
        while (end < document.Text.Length && IsIdentifierCharacter(document.Text[end]))
            end++;

        // A missing name breaks parsing so we add a fake one
        var text = document.Text[..start] + "__completion" + document.Text[end..];
        var parsed = ParsedDocumentStore.ParseDocument(new TextDocumentSnapshot(document.Uri, document.LanguageId, document.Version, text));
        var access = parsed.Root?.Descendants.OfType<MemberAccessExpressionNode>().FirstOrDefault(node => node.Name.Span.Start == start);

        if (access is null)
            return new CompletionList();

        var analysis = await analyses.AnalyzeAsync(parsed, cancellationToken);
        var isInstance = analysis.ExpressionTypes.TryGetValue(access.Receiver, out var type);
        type ??= analysis.GetSymbol(access.Receiver).Symbol as TypeSymbol;

        if (type is null)
            return new CompletionList();

        var enclosingDeclaration = analysis.FirstAncestor<ITypeDeclarationNode>(access) as ASTNode;
        var enclosingType = enclosingDeclaration is null ? null : analysis.GetSymbol(enclosingDeclaration).Symbol as TypeSymbol;
        var prefix = document.Text[start..offset];
        var range = new OmniSharp.Extensions.LanguageServer.Protocol.Models.Range(document.GetPosition(start), document.GetPosition(end));

        return new CompletionList(type.Members.OfType<PropertySymbol>()
            .Where(property => property.IsStatic == !isInstance)
            .Where(property => property.Accessibility != MemberAccessibility.Private || ReferenceEquals(property.ContainingType?.OriginalDefinition, enclosingType?.OriginalDefinition))
            .Where(property => property.Name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            .Select(property => new CompletionItem
            {
                Label = property.Name,
                Kind = CompletionItemKind.Property,
                Detail = $"{property.Type?.Name ?? "?"} {property.Name}",
                InsertText = property.Name,
                TextEdit = new TextEdit { Range = range, NewText = property.Name }
            }));
    }

    private static bool IsIdentifierCharacter(char character) => char.IsLetterOrDigit(character) || character == '_';

    /// <inheritdoc/>
    protected override CompletionRegistrationOptions CreateRegistrationOptions(CompletionCapability capability, ClientCapabilities clientCapabilities)
    {
        return new CompletionRegistrationOptions
        {
            DocumentSelector = new TextDocumentSelector(new TextDocumentFilter { Pattern = "**/*.loom" }),
            TriggerCharacters = new Container<string>(".")
        };
    }
}
