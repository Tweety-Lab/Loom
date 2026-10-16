namespace Loom.Analyzer.Symbols;

/// <summary> A field declaration or a substituted view of its type. </summary>
public class FieldSymbol : MemberSymbol
{
    /// <summary> The resolved type. </summary>
    public virtual TypeSymbol? Type { get; internal set; }

    /// <summary> Initializes a field declaration. </summary>
    public FieldSymbol(string name) : base(name) { }

    /// <summary> Initializes a substituted view of a field declaration. </summary>
    protected FieldSymbol(FieldSymbol definition, TypeSymbol containingType) : base(definition, containingType) { }
}
