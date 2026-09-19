
namespace Loom.Analyzer.Symbols;

public enum MemberAccessibility
{
    /// <summary> No accessibility (i.e., top level) </summary>
    None,

    Public,
    Private
}

public record ModuleSymbol(string Name) : Symbol(Name);

public record TypeSymbol(string Name, TypeSymbol.DefaultType KnownType) : Symbol(Name)
{
    /// <summary> The members declared by this type. </summary>
    public List<Symbol> Members { get; } = new();

    /// <summary> Whether this type is exported for use beyond it's owning module. </summary>
    public bool IsExported { get; set; }

    /// <summary> Whether this type uses value semantics. </summary>
    public bool IsValueType { get; set; }

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

    /// <summary> Whether this method is static. </summary>
    public bool IsStatic { get; set; }

    /// <summary> Whether this method is exported for use beyond it's owning module. </summary>
    public bool IsExported { get; set; }

    /// <summary> The accessibility of this method. </summary>
    public MemberAccessibility Accessibility { get; set; }
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

    /// <summary> The accessibility of this field. </summary>
    public MemberAccessibility Accessibility { get; set; }
}

public record ParameterSymbol(string Name) : Symbol(Name)
{
    /// <summary> The resolved type, bound after all declarations. </summary>
    public TypeSymbol? Type { get; set; }
}