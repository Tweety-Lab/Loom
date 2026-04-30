
namespace Loom.Analyzer.Symbols;

public record ModuleSymbol(string Name) : Symbol(Name);

public record TypeSymbol(string Name, TypeSymbol.DefaultType? KnownType) : Symbol(Name)
{
    public enum DefaultType
    {
        Void,
        I32
    }
}

public record MethodDefinitionSymbol(string Name, TypeSymbol ReturnType, List<ParameterSymbol> Parameters) : Symbol(Name);
public record LocalVariableSymbol(string Name, TypeSymbol Type) : Symbol(Name);
public record ParameterSymbol(string Name, TypeSymbol Type) : Symbol(Name);