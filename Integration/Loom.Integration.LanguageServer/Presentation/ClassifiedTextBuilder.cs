using OmniSharp.Extensions.LanguageServer.Protocol.Models;

using LspRange = OmniSharp.Extensions.LanguageServer.Protocol.Models.Range;

namespace Loom.Integration.LanguageServer.Presentation;

/// <summary>
/// Builds <see cref="ClassifiedText"/>.
/// </summary>
internal sealed class ClassifiedTextBuilder(IClassifiedTextBackend backend)
{
    private readonly List<ClassifiedTextRun> runs = [];

    public ClassifiedTextBuilder Append(TextClassification classification, string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        if (text.Length > 0)
            runs.Add(new(classification, text));

        return this;
    }

    public ClassifiedTextBuilder Append(ClassifiedTextBuilder other)
    {
        runs.AddRange(other.runs);
        return this;
    }

    public ClassifiedTextBuilder Text(string text) => Append(TextClassification.Text, text);
    public ClassifiedTextBuilder Space() => Text(" ");
    public ClassifiedTextBuilder Keyword(string text) => Append(TextClassification.Keyword, text);
    public ClassifiedTextBuilder Namespace(string text) => Append(TextClassification.Namespace, text);
    public ClassifiedTextBuilder ClassName(string text) => Append(TextClassification.Class, text);
    public ClassifiedTextBuilder ParameterName(string text) => Append(TextClassification.Parameter, text);
    public ClassifiedTextBuilder StructName(string text) => Append(TextClassification.Struct, text);
    public ClassifiedTextBuilder MethodName(string text) => Append(TextClassification.Method, text);
    public ClassifiedTextBuilder InterfaceName(string text) => Append(TextClassification.Interface, text);
    public ClassifiedTextBuilder TypeParameter(string text) => Append(TextClassification.TypeParameter, text);
    public ClassifiedTextBuilder Punctuation(string text) => Append(TextClassification.Punctuation, text);

    public ClassifiedText Build() => new([.. runs]);

    public Hover BuildHover(LspRange? range = null) => backend.CreateHover(Build(), range);
}