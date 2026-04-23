namespace Loom.Analyser.Symbols;

public class SymbolTable
{
    /// <summary> The owning <see cref="SymbolTable"/> or null if root. </summary>
    public SymbolTable? Parent { get; }

    private Dictionary<string, Symbol> symbols = new();

    /// <summary> Initializes a new instance of the <see cref="SymbolTable"/> class. </summary>
    public SymbolTable(SymbolTable? parent = null) => Parent = parent;

    /// <summary> Adds a new <see cref="Symbol"/> to the table. </summary>
    public void Define(string name, Symbol symbol) => symbols[name] = symbol;

    /// <summary> Resolves a <see cref="Symbol"/> by name. </summary>
    public Symbol? Resolve(string name) => symbols.TryGetValue(name, out var symbol) ? symbol : Parent?.Resolve(name);
}
