
namespace Loom.Analyzer.Symbols;

public enum MemberAccessibility
{
    /// <summary> No accessibility (i.e., top level) </summary>
    None,

    Public,
    Private
}

public enum PointerType
{
    None,

    Unique,
    Shared,
    Weak
}

/// <summary>
/// Represents a symbol that can be exported.
/// </summary>
public interface IExportableSymbol
{
    /// <summary> Whether this symbol is exported for use beyond it's owning module. </summary>
    bool IsExported { get; set; }
}

/// <summary>
/// Represents a symbol that can have accessibility.
/// </summary>
public interface IAccessibleSymbol
{
    /// <summary> The accessibility of this symbol. </summary>
    MemberAccessibility Accessibility { get; set; }
}

public record ModuleSymbol(string Name) : Symbol(Name);

public record TypeSymbol(string Name, TypeSymbol.DefaultType KnownType) : Symbol(Name), IExportableSymbol
{
    /// <summary> The members declared by this type. </summary>
    public List<Symbol> Members { get; } = new();

    /// <inheritdoc/>
    public bool IsExported { get; set; }

    /// <summary> Whether this type uses value semantics. </summary>
    public bool IsValueType { get; set; }

    /// <summary> Whether this type can only exist on the stack. </summary>
    public bool IsStackOnly { get; set; }

    /// <summary> Whether this type is a generic. </summary>
    public bool IsGeneric => this switch
    {
        TypeParameterSymbol => true,
        ArrayTypeSymbol array => array.ElementType.IsGeneric,
        _ => false
    };

    public enum DefaultType
    {
        Func,
        Void,
        I32,
        I64,
        IPtr,
        Bool,
        Char,
        Struct,
        Class,
        TypeParameter
    }
}

/// <summary> A type declared by a method's type parameter list. </summary>
public record TypeParameterSymbol(string Name) : TypeSymbol(Name, TypeSymbol.DefaultType.TypeParameter);

public record MethodSymbol(string Name, MethodSymbol.MethodKind Kind) : Symbol(Name), IExportableSymbol, IAccessibleSymbol
{
    /// <summary> The resolved return type. </summary>
    public TypeSymbol? ReturnType { get; set; }

    /// <summary> The pointer type for the return value. </summary>
    public PointerType ReturnPointerType { get; set; }

    /// <summary> All type parameters used by this method. </summary>
    public List<TypeParameterSymbol> TypeParameters { get; set; } = new List<TypeParameterSymbol>();

    /// <summary> Whether this method is generic (has type parameters). </summary>
    public bool IsGeneric => TypeParameters.Count > 0;

    /// <summary> The types of this method's parameters, as they are written in its signature. </summary>
    public string Signature => $".{string.Join("_", Parameters.Select(p => p.Type?.LinkageName ?? p.Name))}";

    /// <summary> Whether this method is an overload of another method sharing its name. </summary>
    /// <remarks> An extern method is bound to a symbol outside of Loom, which overloads cannot name. </remarks>
    public bool IsOverloaded => !IsExtern;

    /// <summary> All parameters used by this method. </summary>
    public List<ParameterSymbol> Parameters { get; set; } = new List<ParameterSymbol>();

    /// <summary> Whether this method is static. </summary>
    public bool IsStatic { get; set; }

    /// <summary> Whether this method is extern. </summary>
    public bool IsExtern { get; set; }

    /// <inheritdoc/>
    public bool IsExported { get; set; }

    /// <inheritdoc/>
    public MemberAccessibility Accessibility { get; set; }

    /// <inheritdoc/>
    public override string LinkageName => IsOverloaded ? $"{FullyQualifiedName}{Signature}" : FullyQualifiedName;

    public enum MethodKind
    {
        Constructor,
        Method
    }
}

public record LocalVariableSymbol(string Name) : Symbol(Name)
{
    /// <summary> The resolved type. </summary>
    public TypeSymbol? Type { get; set; }

    /// <summary> The pointer type. </summary>
    public PointerType PointerType { get; set; }
}

public record ArrayTypeSymbol : TypeSymbol
{
    /// <summary> The element type. </summary>
    public TypeSymbol ElementType { get; }

    /// <summary> The size of the array. </summary>
    public int Size { get; }

    public ArrayTypeSymbol(TypeSymbol elementType, int size) : base($"{elementType.Name}[{size}]", elementType.KnownType)
    {
        ElementType = elementType;
        Size = size;
        IsValueType = true;
        FullyQualifiedName = $"{elementType.FullyQualifiedName}[{size}]";
    }
}

public record FieldSymbol(string Name) : Symbol(Name), IAccessibleSymbol
{
    /// <summary> The resolved type. </summary>
    public TypeSymbol? Type { get; set; }

    /// <inheritdoc/>
    public MemberAccessibility Accessibility { get; set; }
}

public record ParameterSymbol(string Name) : Symbol(Name)
{
    /// <summary> The resolved type. </summary>
    public TypeSymbol? Type { get; set; }

    /// <summary> Whether this parameter is a borrow. </summary>
    public bool IsBorrow { get; set; }

    /// <summary> Whether this parameter is mutable. </summary>
    public bool IsMutable { get; set; }
}
