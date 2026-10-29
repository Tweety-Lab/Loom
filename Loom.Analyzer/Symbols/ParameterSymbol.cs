namespace Loom.Analyzer.Symbols;

/// <summary>
/// A parameter declaration or a substituted view of its type.
/// </summary>
public class ParameterSymbol : Symbol
{
    /// <summary> The resolved type. </summary>
    public virtual TypeSymbol? Type { get; internal set; }

    /// <summary> The ownership expected by this parameter. </summary>
    public PointerType PointerType { get; internal set; }

    /// <summary> The method declaration introducing this parameter. </summary>
    public Symbol? DeclaringSymbol { get; internal init; }

    /// <summary> Whether this parameter is a borrow. </summary>
    public bool IsBorrow => PointerType == PointerType.Ref;

    /// <summary> Whether this parameter is mutable. </summary>
    public bool IsMutable { get; internal set; }

    /// <summary> Initializes a parameter declaration. </summary>
    public ParameterSymbol(string name) : base(name) { }

    /// <summary> Initializes a substituted view of a parameter declaration. </summary>
    protected ParameterSymbol(ParameterSymbol definition) : base(definition)
    {
        DeclaringSymbol = definition.DeclaringSymbol;
        PointerType = definition.PointerType;
        IsMutable = definition.IsMutable;
    }
}
