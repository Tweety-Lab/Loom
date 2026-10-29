namespace Loom.Analyzer.Symbols;

/// <summary>
/// A field declaration or a substituted view of its type.
/// </summary>
public class FieldSymbol : MemberSymbol
{
    /// <summary> The resolved type. </summary>
    public virtual TypeSymbol? Type { get; internal set; }

    /// <summary> The ownership of the stored reference. </summary>
    public PointerType PointerType { get; internal set; }

    /// <summary> Whether the referenced value can be mutated through this field. </summary>
    public bool IsMutable { get; internal set; }

    /// <summary> Whether this field belongs to the type rather than an instance. </summary>
    public bool IsStatic { get; set; }

    /// <summary> The compile-time value, or null if not a constant. </summary>
    public int? ConstantValue { get; internal set; } // TODO: Support more constant types

    public bool IsConstant => ConstantValue.HasValue;

    /// <summary> Initializes a field declaration. </summary>
    public FieldSymbol(string name) : base(name) { }

    /// <summary> Initializes a substituted view of a field declaration. </summary>
    protected FieldSymbol(FieldSymbol definition, TypeSymbol containingType) : base(definition, containingType)
    {
        IsStatic = definition.IsStatic;
        PointerType = definition.PointerType;
        IsMutable = definition.IsMutable;
        ConstantValue = definition.ConstantValue;
    }
}
