
namespace Loom.Analyzer.Symbols;

/// <summary>
/// Holds the <see cref="Symbol"/>s bound to an <see cref="Loom.Parser.AST.ASTNode"/>.
/// </summary>
public readonly record struct SymbolInfo
{
    /// <summary> The symbols a node that resolves to nothing is bound to. </summary>
    public static SymbolInfo None { get; } = new([], null);

    /// <summary> Every symbol the node's name could refer to, one per declaration of that name. </summary>
    public IReadOnlyList<Symbol> Candidates { get; }

    /// <summary> The symbol the node refers to, or <see langword="null"/> while its name declares no single symbol. </summary>
    public Symbol? Symbol { get; }

    private SymbolInfo(IReadOnlyList<Symbol> candidates, Symbol? symbol)
    {
        Candidates = candidates;
        Symbol = symbol;
    }

    /// <summary> Binds a node to the single <paramref name="symbol"/> its name declares. </summary>
    public static SymbolInfo Of(Symbol symbol) => new([symbol], symbol);

    /// <summary> Binds a node to every <paramref name="candidates"/> its name could refer to, without choosing between them. </summary>
    public static SymbolInfo OfCandidates(IEnumerable<Symbol> candidates)
    {
        var list = candidates as IReadOnlyList<Symbol> ?? candidates.ToArray();
        return new SymbolInfo(list, list.Count == 1 ? list[0] : null);
    }

    /// <summary> Narrows these candidates down to the <paramref name="symbol"/> the node refers to. </summary>
    public SymbolInfo Resolved(Symbol symbol) => new(Candidates, symbol);

    /// <summary> Reinterprets these candidates as the members of the type the node names. </summary>
    public SymbolInfo AsMembers() => Symbol is TypeSymbol type ? OfCandidates(type.Members) : this;

    /// <summary> Gets the symbol the node refers to as a <typeparamref name="T"/>, if it is one. </summary>
    public T? As<T>() where T : Symbol => Symbol as T;

    /// <summary> Gets every candidate as a <typeparamref name="T"/>, dropping the ones that are not. </summary>
    public IEnumerable<T> AsAll<T>() where T : Symbol => Candidates.OfType<T>();
}
