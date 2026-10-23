using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Loom.Integration.LanguageServer.Presentation;
using Loom.Integration.LanguageServer.Presentation.Generic;
using Loom.Integration.LanguageServer.Documents;
using Loom.Analyzer.Symbols;
using System.Diagnostics.CodeAnalysis;
using Loom.Parser.AST.Rules.Default;

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
            default:
                builder = null;
                return false;
        }
    }

    private bool TryBuildSymbolDocumentation(Symbol symbol, [NotNullWhen(true)] out ClassifiedTextBuilder? builder)
    {
        switch (symbol.DeclaringNode)
        {
            case ClassDeclarationNode type:
                builder = BuildSymbolDocumentation(type.DocumentComments);
                return true;
            default:
                builder = null;
                return false;
        }
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

    private ClassifiedTextBuilder BuildMethodSymbolName(MethodSymbol symbol)
    {
        ClassifiedTextBuilder builder = new ClassifiedTextBuilder(backend);

        builder.Keyword(symbol.IsExported ? "export " : "");
        builder.Keyword(symbol.IsStatic ? "static " : "");
        builder.Keyword(symbol.IsExtern ? "extern " : "");

        if (symbol.ReturnType is not { } returnType)
            builder.Text("? ");
        else if (returnType.IsValueType)
            builder.StructName(returnType.Name + " ");
        else
            builder.ClassName(returnType.Name + " ");

        ReadOnlySpan<string> parts = symbol.FullyQualifiedName.Split("::");

        for (int i = 0; i < parts.Length - 1; i++)
        {
            builder.Namespace(parts[i]);
            builder.Punctuation("::");
        }

        builder.MethodName(parts[^1]);

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
}
