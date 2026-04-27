using Loom.Analyzer.Symbols;
using Loom.Parser.AST;
using Loom.Parser.Rules.Default;

namespace Loom.Analyzer;

internal class DeclarationWalker : ASTVisitor
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

    [Visitor]
    public void Visit(ModuleNode node)
    {
        var symbol = new ModuleSymbol(node.Name);
        CurrentTable.Define(node.Name, symbol);

        WithScope(node, () => VisitChildren(node), symbol);
    }

    [Visitor]
    public void Visit(MethodDefinitionNode node)
    {
        var returnType = CurrentTable.Resolve(node.ReturnType.Value) as TypeSymbol ?? new TypeSymbol(node.ReturnType.Value, null);

        var symbol = new MethodDefinitionSymbol(node.MethodName, returnType);
        CurrentTable.Define(node.MethodName, symbol);

        WithScope(node, () => VisitChildren(node), symbol);
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

    /// <inheritdoc/>
    protected override void OnUnhandled(ASTNode node) => VisitChildren(node);
}
