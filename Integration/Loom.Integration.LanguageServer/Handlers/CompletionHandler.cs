using Loom.Integration.LanguageServer.Documents;
using Loom.Analyzer;
using Loom.Analyzer.Symbols;
using Loom.Parser.AST;
using Loom.Parser.AST.Rules.Default;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Range = OmniSharp.Extensions.LanguageServer.Protocol.Models.Range;

namespace Loom.Integration.LanguageServer.Handlers;

internal sealed class CompletionHandler(ParsedDocumentStore documents, DocumentAnalysisService analyses) : CompletionHandlerBase
{
    /// <inheritdoc/>
    public override Task<CompletionItem> Handle(CompletionItem request, CancellationToken cancellationToken) => Task.FromResult(request);

    /// <inheritdoc/>
    public override async Task<CompletionList> Handle(CompletionParams request, CancellationToken cancellationToken)
    {
        if (!documents.TryGet(request.TextDocument.Uri, out var document))
            return new CompletionList();

        var offset = document.GetOffset(request.Position);
        if (!TryGetMemberCompletionSpan(document.Text, offset, out var start, out var end))
            return new CompletionList();

        // A missing name breaks parsing so we add a fake one
        var text = document.Text[..start] + "__completion" + document.Text[end..];
        var parsed = ParsedDocumentStore.ParseDocument(new TextDocumentSnapshot(document.Uri, document.LanguageId, document.Version, text));
        var access = parsed.Root?.Descendants.OfType<MemberAccessExpressionNode>().FirstOrDefault(node => node.Name.Span.Start == start);

        if (access is null)
            return new CompletionList();

        var analysis = await analyses.AnalyzeAsync(parsed, cancellationToken);
        var prefix = document.Text[start..offset];
        var range = new Range(document.GetPosition(start), document.GetPosition(end));

        return new CompletionList(GetMemberCompletions(access, analysis, prefix, range));
    }

    /// <inheritdoc/>
    protected override CompletionRegistrationOptions CreateRegistrationOptions(CompletionCapability capability, ClientCapabilities clientCapabilities)
    {
        return new CompletionRegistrationOptions
        {
            DocumentSelector = new TextDocumentSelector(new TextDocumentFilter { Pattern = "**/*.loom" }),
            TriggerCharacters = new Container<string>(".")
        };
    }

    private static bool TryGetMemberCompletionSpan(string text, int offset, out int start, out int end)
    {
        start = offset;
        while (start > 0 && IsIdentifierCharacter(text[start - 1]))
            start--;

        var dot = start - 1;
        while (dot >= 0 && char.IsWhiteSpace(text[dot]))
            dot--;

        end = offset;
        while (end < text.Length && IsIdentifierCharacter(text[end]))
            end++;

        return dot >= 0 && text[dot] == '.';
    }

    private static IEnumerable<CompletionItem> GetMemberCompletions(MemberAccessExpressionNode access, AnalysisContext analysis, string prefix, Range range)
    {
        var isInstance = analysis.ExpressionTypes.TryGetValue(access.Receiver, out var type);
        type ??= analysis.GetSymbol(access.Receiver).Symbol as TypeSymbol;

        if (type is null)
            yield break;

        var enclosingDeclaration = analysis.FirstAncestor<ITypeDeclarationNode>(access) as ASTNode;
        var enclosingType = enclosingDeclaration is null ? null : analysis.GetSymbol(enclosingDeclaration).Symbol as TypeSymbol;

        foreach (var symbol in type.Members)
        {
            if (!symbol.Name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                continue;

            if (symbol is MemberSymbol { Accessibility: MemberAccessibility.Private } member && !ReferenceEquals(member.ContainingType?.OriginalDefinition, enclosingType?.OriginalDefinition))
                continue;

            if (BuildSymbolCompletion(symbol, isInstance, range) is { } completion)
                yield return completion;
        }
    }

    private static CompletionItem? BuildSymbolCompletion(Symbol symbol, bool isInstance, Range range) => symbol switch
    {
        PropertySymbol property => BuildPropertyCompletion(property, range),
        MethodSymbol method when method.Kind == MethodSymbol.MethodKind.Method => BuildMethodCompletion(method, range),
        _ => null
    };

    private static CompletionItem BuildPropertyCompletion(PropertySymbol property, Range range) => new()
    {
        Label = property.Name,
        Kind = CompletionItemKind.Property,
        Detail = $"{(property.IsStatic ? "static " : "")}{property.Type?.Name ?? "?"} {property.Name}",
        InsertText = property.Name,
        TextEdit = new TextEdit { Range = range, NewText = property.Name }
    };

    private static CompletionItem BuildMethodCompletion(MethodSymbol method, Range range) => new()
    {
        Label = method.Name,
        Kind = CompletionItemKind.Method,
        Detail = $"{method.ReturnType?.Name ?? "?"} {method.Name}()",
        InsertText = method.Name,
        TextEdit = new TextEdit { Range = range, NewText = method.Name }
    };
    private static bool IsIdentifierCharacter(char character) => char.IsLetterOrDigit(character) || character == '_';
}
