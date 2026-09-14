
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

public record MethodSymbol(string Name, List<ParameterSymbol> Parameters) : Symbol(Name)
{
    /// <summary> The resolved return type, bound after all declarations. </summary>
    public TypeSymbol? ReturnType { get; set; }
}

public record LocalVariableSymbol(string Name) : Symbol(Name)
{
    /// <summary> The resolved type, bound after all declarations. </summary>
    public TypeSymbol? Type { get; set; }
}

public record FieldSymbol(string Name) : Symbol(Name)
{
    /// <summary> The resolved type, bound after all declarations. </summary>
    public TypeSymbol? Type { get; set; }
}

public record ParameterSymbol(string Name) : Symbol(Name)
{
    /// <summary> The resolved type, bound after all declarations. </summary>
    public TypeSymbol? Type { get; set; }
}