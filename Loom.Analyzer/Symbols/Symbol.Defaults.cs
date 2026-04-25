
namespace Loom.Analyzer.Symbols;

public record ModuleSymbol(string Name) : Symbol(Name);

public record TypeSymbol(string Name, TypeSymbol.Type? KnownType) : Symbol(Name)
{
    public enum Type
    {
        Void,
        I32
    }
}

public record MethodDefinitionSymbol(string Name, TypeSymbol ReturnType) : Symbol(Name);
