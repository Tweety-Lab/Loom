using System.Collections.Immutable;

namespace Loom.Integration.LanguageServer.Presentation;

internal enum TextClassification
{
    Text,
    Keyword,
    Namespace,
    Class,
    Struct,
    Enum,
    Interface,
    TypeParameter,
    Method,
    Property,
    Field,
    Parameter,
    Local,
    Punctuation,
    Number,
    String
}

internal readonly record struct ClassifiedTextRun(TextClassification Classification, string Text);

/// <summary>
/// An immutable snapshot of classified text.
/// </summary>
internal sealed record ClassifiedText(ImmutableArray<ClassifiedTextRun> Runs)
{
    public string Text => string.Concat(Runs.Select(run => run.Text));
}