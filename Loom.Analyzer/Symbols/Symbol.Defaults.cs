
namespace Loom.Analyzer.Symbols;

public record ModuleSymbol(string Name) : Symbol(Name);
public record MethodDefinitionSymbol(string Name) : Symbol(Name);
