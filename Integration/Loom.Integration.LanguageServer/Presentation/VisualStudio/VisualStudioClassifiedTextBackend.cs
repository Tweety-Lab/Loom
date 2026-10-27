using Loom.Integration.LanguageServer.Presentation.Generic;
using Newtonsoft.Json;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using LspRange = OmniSharp.Extensions.LanguageServer.Protocol.Models.Range;

namespace Loom.Integration.LanguageServer.Presentation.VisualStudio;

/// <summary>
/// Visual Studio implementation of <see cref="IClassifiedTextBackend"/> that uses VS stuff.
/// </summary>
internal sealed class VisualStudioClassifiedTextBackend : IClassifiedTextBackend
{
    public Hover CreateHover(ClassifiedText text, LspRange? range = null)
    {
        var fallback = new GenericClassifiedTextBackend().CreateHover(text, range);
        return new VisualStudioHover
        {
            Contents = fallback.Contents,
            Range = range,
            RawContent = new ContainerElement([CreateTextElement(text)])
        };
    }

    public CompletionItem CreateCompletionItem(ClassifiedText? signature, ClassifiedText? documentation)
    {
        var fallback = new GenericClassifiedTextBackend().CreateCompletionItem(signature, documentation);
        var builder = new ClassifiedTextBuilder(this);
        if (signature is not null)
            foreach (var run in signature.Runs)
                builder.Append(run.Classification, run.Text);

        if (documentation is not null)
        {
            if (signature is not null)
                builder.Punctuation("\n");

            foreach (var run in documentation.Runs)
                builder.Append(run.Classification, run.Text);
        }

        return new VisualStudioCompletionItem
        {
            Detail = fallback.Detail,
            Documentation = fallback.Documentation,
            Description = CreateTextElement(builder.Build())
        };
    }

    private static TextElement CreateTextElement(ClassifiedText text) => new([.. text.Runs.Select(run => new TextRun(ClassificationName(run.Classification), run.Text))]);

    private static string ClassificationName(TextClassification classification) => classification switch
    {
        TextClassification.Text => "text",
        TextClassification.Keyword => "keyword",
        TextClassification.Namespace => "namespace name",
        TextClassification.Class => "class name",
        TextClassification.Struct => "struct name",
        TextClassification.Enum => "enum name",
        TextClassification.Interface => "interface name",
        TextClassification.TypeParameter => "type parameter name",
        TextClassification.Method => "method name",
        TextClassification.Property => "property name",
        TextClassification.Field => "field name",
        TextClassification.Parameter => "parameter name",
        TextClassification.Local => "local name",
        TextClassification.Punctuation => "punctuation",
        TextClassification.Number => "number",
        TextClassification.String => "string",
        _ => throw new ArgumentOutOfRangeException(nameof(classification), classification, null)
    };

    private record VisualStudioHover : Hover
    {
        [JsonProperty("_vs_rawContent")]
        public required ContainerElement RawContent { get; init; }
    }

    private record VisualStudioCompletionItem : CompletionItem
    {
        [JsonProperty("_vs_description")]
        public required TextElement Description { get; init; }
    }

    private sealed record ContainerElement([property: JsonProperty("Elements")] TextElement[] Elements)
    {
        [JsonProperty("_vs_type")] public string Type => "ContainerElement";
        [JsonProperty("Style")] public int Style => 0;
    }

    private sealed record TextElement([property: JsonProperty("Runs")] TextRun[] Runs)
    {
        [JsonProperty("_vs_type")] public string Type => "ClassifiedTextElement";
    }

    private sealed record TextRun([property: JsonProperty("ClassificationTypeName")] string ClassificationTypeName, [property: JsonProperty("Text")] string Text)
    {
        [JsonProperty("_vs_type")] public string Type => "ClassifiedTextRun";
        [JsonProperty("Style")] public int Style => 0;
    }
}