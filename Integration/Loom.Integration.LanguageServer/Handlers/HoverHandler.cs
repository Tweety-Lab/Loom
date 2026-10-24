using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Loom.Integration.LanguageServer.Presentation;
using Loom.Integration.LanguageServer.Presentation.Generic;
using Loom.Integration.LanguageServer.Documents;
using Loom.Analyzer.Symbols;
using System.Diagnostics.CodeAnalysis;
using Loom.Parser.AST.Rules.Default;
using Loom.Parser.AST;

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

        if (TryBuildSymbolDocumentation(symbol, out var documentation))
        {
            builder.Append(TextClassification.Punctuation, "\n");
            builder.Append(documentation);
        }

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
            case MethodSymbol method:
                builder = BuildMethodSymbolName(method);
                return true;
            case PropertySymbol property:
                builder = BuildPropertySymbolName(property);
                return true;
            default:
                builder = null;
                return false;
        }
    }

    private bool TryBuildSymbolDocumentation(Symbol symbol, [NotNullWhen(true)] out ClassifiedTextBuilder? builder)
    {
        builder = null;

        if (symbol.DeclaringNode is not IDocumentableNode node)
            return false;

        if (!node.DocumentComments.Items.Any(comment => comment.Directive is DocumentCommentDirective.Brief or DocumentCommentDirective.Remark && !string.IsNullOrWhiteSpace(comment.Body)))
            return false;

        builder = BuildSymbolDocumentation(node.DocumentComments);
        return true;
    }

    private ClassifiedTextBuilder BuildSymbolDocumentation(DocumentCommentListNode documentation)
    {
        ClassifiedTextBuilder builder = new ClassifiedTextBuilder(backend);

        foreach (var comment in documentation.Items)
        {
            switch (comment.Directive)
            {
                case DocumentCommentDirective.Brief:
                    builder.Text(comment.Body);
                    break;
                case DocumentCommentDirective.Remark:
                    builder.Append(TextClassification.Punctuation, "\n\n");
                    builder.Text(comment.Body);
                    break;
                case DocumentCommentDirective.Return:
                    builder.Append(TextClassification.Punctuation, "\n\n");
                    builder.Keyword("Returns:\n");
                    builder.Text($"     {comment.Body}");
                    break;
            }
        }

        return builder;
    }

    private ClassifiedTextBuilder BuildTypeSymbolName(TypeSymbol symbol)
    {
        ClassifiedTextBuilder builder = new ClassifiedTextBuilder(backend);

        builder.Keyword(symbol.IsExported ? "export " : "");
        builder.Keyword(symbol.IsStatic ? "static " : "");
        builder.Keyword(symbol.IsStackOnly ? "stack " : "");

        builder.Keyword(symbol.IsValueType ? "struct " : "class ");

        builder.Append(BuildFullyQualifiedName(symbol));

        return builder;
    }

    private ClassifiedTextBuilder BuildPropertySymbolName(PropertySymbol symbol)
    {
        ClassifiedTextBuilder builder = new ClassifiedTextBuilder(backend);

        switch (symbol.Accessibility)
        {
            case MemberAccessibility.Public:
                builder.Keyword("public ");
                break;
            case MemberAccessibility.Private:
                builder.Keyword("private ");
                break;
        }

        builder.Keyword(symbol.IsStatic ? "static " : "");

        if (symbol.BackingField?.Type is not { } returnType)
            builder.Text("? ");
        else if (returnType.IsValueType)
            builder.StructName(returnType.Name + " ");
        else
            builder.ClassName(returnType.Name + " ");

        builder.Append(BuildFullyQualifiedName(symbol));

        builder.Punctuation(" { ");

        if (symbol.Getter is not null)
        {
            builder.Keyword("get");
            builder.Punctuation("; ");
        }
        if (symbol.Setter is not null)
        {
            builder.Keyword("set");
            builder.Punctuation("; ");
        }

        builder.Punctuation("}");

        return builder;
    }

    private ClassifiedTextBuilder BuildMethodSymbolName(MethodSymbol symbol)
    {
        ClassifiedTextBuilder builder = new ClassifiedTextBuilder(backend);

        switch (symbol.Accessibility)
        {
            case MemberAccessibility.Public:
                builder.Keyword("public ");
                break;
            case MemberAccessibility.Private:
                builder.Keyword("private ");
                break;
        }

        builder.Keyword(symbol.IsExported ? "export " : "");
        builder.Keyword(symbol.IsStatic ? "static " : "");
        builder.Keyword(symbol.IsExtern ? "extern " : "");

        if (symbol.ReturnType is not { } returnType)
            builder.Text("? ");
        else if (returnType.IsValueType)
            builder.StructName(returnType.Name + " ");
        else
            builder.ClassName(returnType.Name + " ");

        builder.Append(BuildFullyQualifiedName(symbol));

        builder.Punctuation("(");

        bool first = true;
        foreach (var param in symbol.Parameters)
        {
            if (!first)
                builder.Punctuation(", ");

            if (param.Type is not { } parameterType)
                builder.Text("? ");
            else if (parameterType.IsValueType)
                builder.StructName(parameterType.Name + " ");
            else
                builder.ClassName(parameterType.Name + " ");

            builder.ParameterName(param.Name);

            first = false;
        }

        builder.Punctuation(")");

        return builder;
    }

    private ClassifiedTextBuilder BuildFullyQualifiedName(Symbol symbol)
    {
        ClassifiedTextBuilder builder = new ClassifiedTextBuilder(backend);

        ReadOnlySpan<string> parts = symbol.FullyQualifiedName.Split("::");

        for (int i = 0; i < parts.Length - 1; i++)
        {
            builder.Namespace(parts[i]);
            builder.Punctuation("::");
        }

        switch (symbol)
        {
            case MethodSymbol method:
                builder.MethodName(parts[^1]);
                break;
            case PropertySymbol property:
                builder.PropertyName(parts[^1]);
                break;
            case TypeSymbol type:
                if (type.IsValueType)
                    builder.StructName(parts[^1]);
                else
                    builder.ClassName(parts[^1]);
                break;
            case FieldSymbol field:
                builder.FieldName(parts[^1]);
                break;
            default:
                builder.Text(parts[^1]);
                break;
        }

        return builder;
    }
}
