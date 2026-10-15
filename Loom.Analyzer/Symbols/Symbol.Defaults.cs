
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

/// <summary>
/// Represents a symbol that can have type parameters.
/// </summary>
public interface ITypeParameterizableSymbol
{
    /// <summary> The type parameters of this symbol. </summary>
    List<TypeParameterSymbol> TypeParameters { get; }
}

public record ModuleSymbol(string Name) : Symbol(Name);

public record TypeSymbol(string Name, TypeSymbol.DefaultType KnownType) : Symbol(Name), IExportableSymbol, ITypeParameterizableSymbol
{
    private readonly List<Symbol> members = new();

    /// <summary> The members declared by this type. </summary>
    public virtual List<Symbol> Members => members;

    /// <inheritdoc/>
    public List<TypeParameterSymbol> TypeParameters { get; } = new();

    /// <inheritdoc/>
    public bool IsExported { get; set; }

    /// <summary> Whether this type uses value semantics. </summary>
    public bool IsValueType { get; set; }

    /// <summary> Whether this type can only exist on the stack. </summary>
    public bool IsStackOnly { get; set; }

    /// <summary> Whether this type is a generic, i.e. it still has unresolved type parameters. </summary>
    public bool IsGeneric => this switch
    {
        TypeParameterSymbol => true,
        ArrayTypeSymbol array => array.ElementType.IsGeneric,
        _ => TypeParameters.Count > 0
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

public record TypeParameterSymbol(string Name) : TypeSymbol(Name, TypeSymbol.DefaultType.TypeParameter);

public record MethodSymbol(string Name, MethodSymbol.MethodKind Kind) : Symbol(Name), IExportableSymbol, IAccessibleSymbol, ITypeParameterizableSymbol
{
    /// <summary> The resolved return type. </summary>
    public TypeSymbol? ReturnType { get; set; }

    /// <summary> The pointer type for the return value. </summary>
    public PointerType ReturnPointerType { get; set; }

    /// <inheritdoc/>
    public List<TypeParameterSymbol> TypeParameters { get; set; } = new List<TypeParameterSymbol>();

    /// <summary> Whether this method is generic (has type parameters). </summary>
    public bool IsGeneric => TypeParameters.Count > 0;

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

    /// <summary> The generic member this method was substituted from, or null when it does not belong to an instantiated generic type. </summary>
    public MethodSymbol? GenericDefinition { get; set; }

    public enum MethodKind
    {
        Constructor,
        Accessor,
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

/// <summary>
/// A generic type whose type parameters have been bound to concrete type arguments, i.e. an instantiation such as <c>Slice&lt;char&gt;</c>.
/// </summary>
public record InstantiatedTypeSymbol : TypeSymbol
{
    /// <summary> The generic type this instantiation was built from. </summary>
    public TypeSymbol Definition { get; }

    /// <summary> The concrete type arguments the definition's type parameters were bound to. </summary>
    public IReadOnlyList<TypeSymbol> TypeArguments { get; }

    /// <summary> The substitution binding the definition's type parameters to <see cref="TypeArguments"/>. </summary>
    public TypeSubstitution Substitution { get; }

    private readonly List<Symbol> members = new();
    private bool membersBuilt;

    /// <summary>
    /// The definition's members with their types substituted for the type arguments.
    /// </summary>
    /// <remarks> Built lazily so that the definition's members are read after every declaration has been bound. </remarks>
    public override List<Symbol> Members
    {
        get
        {
            if (!membersBuilt)
            {
                membersBuilt = true;
                members.AddRange(Definition.Members.Select(member => Substitute(member)));
            }

            return members;
        }
    }

    /// <summary> Initializes a new instance of the <see cref="InstantiatedTypeSymbol"/> class. </summary>
    public InstantiatedTypeSymbol(TypeSymbol definition, IReadOnlyList<TypeSymbol> typeArguments, string name, TypeSubstitution substitution) : base(name, definition.KnownType)
    {
        Definition = definition;
        TypeArguments = typeArguments;
        Substitution = substitution;

        IsValueType = definition.IsValueType;
        IsStackOnly = definition.IsStackOnly;
        IsExported = definition.IsExported;
    }

    // Builds a copy of a member whose types have the definition's type parameters substituted for the instantiation's arguments
    private Symbol Substitute(Symbol member)
    {
        switch (member)
        {
            case FieldSymbol field:
                return SubstituteField(field);

            case PropertySymbol property:
                var substitutedProperty = new PropertySymbol(property.Name)
                {
                    Type = property.Type != null ? Substitution.Resolve(property.Type) : null,
                    Accessibility = property.Accessibility,
                    IsStatic = property.IsStatic,
                    DeclaringNode = property.DeclaringNode
                };
                substitutedProperty.FullyQualifiedName = $"{FullyQualifiedName}::{property.Name}";
                substitutedProperty.BackingField = property.BackingField != null ? SubstituteField(property.BackingField) : null;
                substitutedProperty.Getter = property.Getter != null ? SubstituteMethod(property.Getter) : null;
                substitutedProperty.Setter = property.Setter != null ? SubstituteMethod(property.Setter) : null;
                return substitutedProperty;

            case MethodSymbol method:
                return SubstituteMethod(method);

            default:
                return member;
        }
    }

    private FieldSymbol SubstituteField(FieldSymbol field)
    {
        var substituted = new FieldSymbol(field.Name)
        {
            Type = field.Type != null ? Substitution.Resolve(field.Type) : null,
            Accessibility = field.Accessibility,
            DeclaringNode = field.DeclaringNode
        };
        substituted.FullyQualifiedName = $"{FullyQualifiedName}::{field.Name}";
        return substituted;
    }

    private MethodSymbol SubstituteMethod(MethodSymbol method)
    {
        var substituted = new MethodSymbol(method.Name, method.Kind)
        {
            ReturnType = method.ReturnType != null ? Substitution.Resolve(method.ReturnType) : null,
            ReturnPointerType = method.ReturnPointerType,
            IsStatic = method.IsStatic,
            IsExtern = method.IsExtern,
            IsExported = method.IsExported,
            Accessibility = method.Accessibility,
            DeclaringNode = method.DeclaringNode,
            TypeParameters = method.TypeParameters,
            Parameters = method.Parameters.Select(parameter => new ParameterSymbol(parameter.Name)
            {
                Type = parameter.Type != null ? Substitution.Resolve(parameter.Type) : null,
                IsBorrow = parameter.IsBorrow,
                IsMutable = parameter.IsMutable
            }).ToList()
        };
        substituted.FullyQualifiedName = $"{FullyQualifiedName}::{method.Name}";
        substituted.GenericDefinition = method;
        return substituted;
    }
}

public record FieldSymbol(string Name) : Symbol(Name), IAccessibleSymbol
{
    /// <summary> The resolved type. </summary>
    public TypeSymbol? Type { get; set; }

    /// <inheritdoc/>
    public MemberAccessibility Accessibility { get; set; }
}

public record PropertySymbol(string Name) : Symbol(Name), IAccessibleSymbol
{
    /// <summary> The resolved type. </summary>
    public TypeSymbol? Type { get; set; }

    /// <inheritdoc/>
    public MemberAccessibility Accessibility { get; set; }

    /// <summary> Whether this property is static. </summary>
    public bool IsStatic { get; set; }

    /// <summary> The hidden field the property's accessors read and write. </summary>
    public FieldSymbol? BackingField { get; set; }

    /// <summary> The accessor that reads the property, or null when it declares no get accessor. </summary>
    public MethodSymbol? Getter { get; set; }

    /// <summary> The accessor that writes the property, or null when it declares no set accessor. </summary>
    public MethodSymbol? Setter { get; set; }
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
