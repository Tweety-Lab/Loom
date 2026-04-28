
namespace Loom.Analyzer.Symbols;

public record ModuleSymbol(string Name) : Symbol(Name);

public record TypeSymbol(string Name, TypeSymbol.KnownType? Type) : Symbol(Name)
{
    public enum KnownType
    {
        Void,
        I32
    }
}

public record MethodDefinitionSymbol(string Name, TypeSymbol ReturnType) : Symbol(Name);
