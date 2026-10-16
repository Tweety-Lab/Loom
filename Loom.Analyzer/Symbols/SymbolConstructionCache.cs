using System.Collections.Immutable;

namespace Loom.Analyzer.Symbols;

/// <summary> Interns immutable generic argument lists within a single declaration. </summary>
internal sealed class SymbolConstructionCache<TSymbol> where TSymbol : Symbol
{
    private readonly Dictionary<ImmutableArray<TypeSymbol>, TSymbol> symbols = new(TypeArgumentComparer.Instance);

    /// <summary> The semantic constructions requested for this declaration. </summary>
    public IEnumerable<TSymbol> Symbols => symbols.Values;

    /// <summary> Gets the canonical symbol for <paramref name="arguments"/>, creating it on first use. </summary>
    public TSymbol GetOrCreate(ImmutableArray<TypeSymbol> arguments, Func<ImmutableArray<TypeSymbol>, TSymbol> create)
    {
        if (!symbols.TryGetValue(arguments, out var symbol))
            symbols.Add(arguments, symbol = create(arguments));

        return symbol;
    }

    private sealed class TypeArgumentComparer : IEqualityComparer<ImmutableArray<TypeSymbol>>
    {
        public static TypeArgumentComparer Instance { get; } = new();

        public bool Equals(ImmutableArray<TypeSymbol> left, ImmutableArray<TypeSymbol> right) => left.Length == right.Length && left.Zip(right).All(pair => TypeConversions.IsIdentical(pair.First, pair.Second));

        public int GetHashCode(ImmutableArray<TypeSymbol> arguments)
        {
            HashCode hash = new();

            foreach (var argument in arguments)
                hash.Add(TypeConversions.IdentityHashCode(argument));

            return hash.ToHashCode();
        }
    }
}
