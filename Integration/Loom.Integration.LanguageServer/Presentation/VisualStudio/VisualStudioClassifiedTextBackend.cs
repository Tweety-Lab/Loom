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
            RawContent = new ContainerElement([new TextElement([.. text.Runs.Select(run => new TextRun(ClassificationName(run.Classification), run.Text))])])
        };
    }

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

    private sealed record ContainerElement([property: JsonProperty("Elements")] TextElement[] Elements)
    {
        [JsonProperty("_vs_type")] public string Type => "ContainerElement";
        [JsonProperty("Style")] public int Style => 0;
    }

    private sealed record TextElement([property: JsonProperty("Runs")] TextRun[] Runs)
    {
        [JsonProperty("_vs_type")] public string Type => "ClassifiedTextElement";
    }

    private sealed record TextRun(
        [property: JsonProperty("ClassificationTypeName")] string ClassificationTypeName,
        [property: JsonProperty("Text")] string Text)
    {
        [JsonProperty("_vs_type")] public string Type => "ClassifiedTextRun";
        [JsonProperty("Style")] public int Style => 0;
    }
}