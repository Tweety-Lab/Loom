namespace Loom.Analyzer.Symbols;

/// <summary> A local variable declaration and its resolved storage type. </summary>
public class LocalVariableSymbol(string Name) : Symbol(Name)
{
    /// <summary> The resolved type. </summary>
    public TypeSymbol? Type { get; set; }

    /// <summary> The pointer type. </summary>
    public PointerType PointerType { get; set; }
}
