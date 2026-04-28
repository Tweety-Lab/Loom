namespace Loom.Analyzer.Symbols;

/// <summary>
/// A Scoped set of <see cref="Symbol"/>s.
/// </summary>
public class Binder
{
    /// <summary> The owning <see cref="Binder"/> or null if root. </summary>
    public Binder? Parent { get; }

    /// <summary> All symbols in the table. </summary>
    public IReadOnlySet<Symbol> Symbols => symbols;

    private HashSet<Symbol> symbols = new();

    /// <summary> Initializes a new instance of the <see cref="Binder"/> class. </summary>
    public Binder(Binder? parent = null) => Parent = parent;

    /// <summary> Adds a new <see cref="Symbol"/> to the table. </summary>
    public void Define(Symbol symbol) => symbols.Add(symbol);

    /// <summary> Resolves a set of <see cref="Symbol"/> by name. </summary>
    public IEnumerable<Symbol>? Lookup(string name)
    {
        if (symbols.Any(symbol => symbol.Name == name))
            return symbols.Where(symbol => symbol.Name == name);

        return Parent?.Lookup(name);
    }
}
