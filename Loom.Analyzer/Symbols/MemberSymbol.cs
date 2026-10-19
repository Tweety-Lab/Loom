namespace Loom.Analyzer.Symbols;

/// <summary>
/// A method, field, or property declared by a type.
/// </summary>
public abstract class MemberSymbol : Symbol, IAccessibleSymbol
{
    /// <summary> The type through which this member is viewed, or null for a module-level method. </summary>
    public TypeSymbol? ContainingType { get; internal set; }

    /// <inheritdoc/>
    public MemberAccessibility Accessibility { get; set; }

    /// <summary> Initializes a member declaration. </summary>
    protected MemberSymbol(string name) : base(name) { }

    /// <summary> Initializes a substituted member in <paramref name="containingType"/>. </summary>
    protected MemberSymbol(MemberSymbol definition, TypeSymbol? containingType) : base(definition)
    {
        ContainingType = containingType;
        Accessibility = definition.Accessibility;
        FullyQualifiedName = containingType == null ? definition.FullyQualifiedName : $"{containingType.FullyQualifiedName}::{Name}";
    }
}
