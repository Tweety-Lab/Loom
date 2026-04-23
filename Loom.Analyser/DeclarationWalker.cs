using Loom.Analyser.Symbols;
using Loom.Parser.AST;
using Loom.Parser.Rules.Default;

namespace Loom.Analyser;

internal class DeclarationWalker : ASTWalker
{
    /// <summary> Maps <see cref="ASTNode"/>s to their corresponding <see cref="SymbolTable"/>. </summary>
    public Dictionary<ASTNode, SymbolTable> SymbolTables { get; }

    /// <summary> The current <see cref="SymbolTable"/>. </summary>
    public SymbolTable CurrentTable { get; private set; }

    /// <summary> The current <see cref="Symbol"/>. </summary>
    public Symbol? CurrentSymbol { get; private set; }

    /// <summary> Initializes a new instance of the <see cref="DeclarationWalker"/> class. </summary>
    public DeclarationWalker(Dictionary<ASTNode, SymbolTable> symbolTables, SymbolTable currentTable)
    {
        SymbolTables = symbolTables;
        CurrentTable = currentTable;
    }

    /// <inheritdoc/>
    public override void Visit(ModuleNode node)
    {
        var symbol = new ModuleSymbol(node.Name);
        CurrentTable.Define(node.Name, symbol);

        WithScope(node, () => WalkChildren(node), symbol);
    }

    private void WithScope(ASTNode node, Action body, Symbol? symbol = null)
    {
        var parentTable = CurrentTable;
        var previousSymbol = CurrentSymbol;

        CurrentTable = new SymbolTable(parentTable);
        SymbolTables[node] = CurrentTable;

        if (symbol is not null)
            CurrentSymbol = symbol;

        body();

        CurrentTable = parentTable;
        CurrentSymbol = previousSymbol;
    }
}
