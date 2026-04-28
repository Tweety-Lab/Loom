namespace Loom.Analyzer.Symbols;

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

    /// <summary> Resolves a <see cref="Symbol"/> by name. </summary>
    public Symbol? Lookup(string name)
    {
        foreach (var symbol in symbols)
            if (symbol.Name == name)
                return symbol;

        return Parent?.Lookup(name);
    }
}
