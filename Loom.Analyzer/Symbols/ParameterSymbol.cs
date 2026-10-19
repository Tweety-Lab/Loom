namespace Loom.Analyzer.Symbols;

/// <summary>
/// A parameter declaration or a substituted view of its type.
/// </summary>
public class ParameterSymbol : Symbol
{
    /// <summary> The resolved type. </summary>
    public virtual TypeSymbol? Type { get; internal set; }

    /// <summary> Whether this parameter is a borrow. </summary>
    public bool IsBorrow { get; set; }

    /// <summary> Whether this parameter is mutable. </summary>
    public bool IsMutable { get; set; }

    /// <summary> Initializes a parameter declaration. </summary>
    public ParameterSymbol(string name) : base(name) { }

    /// <summary> Initializes a substituted view of a parameter declaration. </summary>
    protected ParameterSymbol(ParameterSymbol definition) : base(definition)
    {
        IsBorrow = definition.IsBorrow;
        IsMutable = definition.IsMutable;
    }
}
