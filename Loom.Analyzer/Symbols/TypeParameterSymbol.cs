namespace Loom.Analyzer.Symbols;

/// <summary> A generic parameter identified by its declaration and ordinal, independent of its spelling. </summary>
public sealed class TypeParameterSymbol(string name) : TypeSymbol(name, TypeSymbol.DefaultType.TypeParameter)
{
    /// <summary> The type or method declaration introducing this parameter. </summary>
    public Symbol? DeclaringSymbol { get; internal init; }

    /// <summary> The zero-based position within the declaring symbol's own type parameters. </summary>
    public int Ordinal { get; internal init; }

    /// <inheritdoc/>
    public override bool ContainsTypeParameters => true;
}
