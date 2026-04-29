
namespace Loom.Analyzer.Symbols;

public abstract record Symbol(string Name)
{
    public string? FullyQualifiedName { get; set; }
}
