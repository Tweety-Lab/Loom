
namespace Loom.Analyzer.Symbols;

public enum MemberAccessibility
{
    /// <summary> No accessibility (i.e., top level) </summary>
    None,

    Public,
    Private
}

/// <summary>
/// Represents a symbol that can be exported.
/// </summary>
public interface IExportable
{
    /// <summary> Whether this symbol is exported for use beyond it's owning module. </summary>
    bool IsExported { get; set; }
}

public record ModuleSymbol(string Name) : Symbol(Name);

public record TypeSymbol(string Name, TypeSymbol.DefaultType KnownType) : Symbol(Name), IExportable
{
    /// <summary> The members declared by this type. </summary>
    public List<Symbol> Members { get; } = new();

    /// <inheritdoc/>
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
        Char,
        Struct
    }
}

public record MethodSymbol(string Name, List<ParameterSymbol> Parameters) : Symbol(Name), IExportable
{
    /// <summary> The resolved return type, bound after all declarations. </summary>
    public TypeSymbol? ReturnType { get; set; }

    /// <summary> Whether this method is static. </summary>
    public bool IsStatic { get; set; }

    /// <inheritdoc/>
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