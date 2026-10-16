namespace Loom.Analyzer.Symbols;

/// <summary> A declaration with positional type parameters and a possibly substituted set of arguments. </summary>
public interface IGenericSymbol
{
    /// <summary> The type parameters introduced by this declaration, in ordinal order. </summary>
    IReadOnlyList<TypeParameterSymbol> TypeParameters { get; }

    /// <summary> The supplied arguments, or the declaration's own parameters for an unconstructed symbol. </summary>
    IReadOnlyList<TypeSymbol> TypeArguments { get; }

    /// <summary> The number of type parameters introduced by this declaration. </summary>
    int Arity => TypeParameters.Count;

    /// <summary> Whether this declaration introduces type parameters. </summary>
    bool IsGeneric => Arity > 0;

    /// <summary> Whether any argument or containing type still depends on a type parameter. </summary>
    bool ContainsTypeParameters { get; }
}
