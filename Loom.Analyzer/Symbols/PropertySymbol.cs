namespace Loom.Analyzer.Symbols;

/// <summary>
/// A property declaration with its backing field and accessors.
/// </summary>
public class PropertySymbol : MemberSymbol
{
    /// <summary> The resolved type. </summary>
    public virtual TypeSymbol? Type { get; internal set; }

    /// <summary> The ownership of the stored reference. </summary>
    public PointerType PointerType { get; internal set; }

    /// <summary> Whether the referenced value can be mutated through this property. </summary>
    public bool IsMutable { get; internal set; }

    /// <summary> Whether this property is static. </summary>
    public bool IsStatic { get; set; }

    /// <summary> The hidden field the property's auto accessors read and write. </summary>
    public virtual FieldSymbol? BackingField { get; internal set; }

    /// <summary> The accessor that reads the property, or null when it declares no get accessor. </summary>
    public virtual MethodSymbol? Getter { get; internal set; }

    /// <summary> The accessor that writes the property, or null when it declares no set accessor. </summary>
    public virtual MethodSymbol? Setter { get; internal set; }

    /// <summary> Initializes a property declaration. </summary>
    public PropertySymbol(string name) : base(name) { }

    /// <summary> Initializes a substituted view of a property declaration. </summary>
    protected PropertySymbol(PropertySymbol definition, TypeSymbol containingType) : base(definition, containingType)
    {
        IsStatic = definition.IsStatic;
        PointerType = definition.PointerType;
        IsMutable = definition.IsMutable;
    }
}
