
namespace Loom.Analyzer.Symbols;

public record ModuleSymbol(string Name) : Symbol(Name);

public record TypeSymbol(string Name, TypeSymbol.DefaultType KnownType) : Symbol(Name)
{
    /// <summary> The members declared by this type. </summary>
    public List<Symbol> Members { get; } = new();

    public enum DefaultType
    {
        Func,
        Void,
        I32,
        IPtr,
        Bool,
        Struct
    }
}

public record MethodDefinitionSymbol(string Name, TypeSymbol ReturnType, List<ParameterSymbol> Parameters) : Symbol(Name);
public record LocalVariableSymbol(string Name, TypeSymbol Type) : Symbol(Name);
public record ParameterSymbol(string Name, TypeSymbol Type) : Symbol(Name);