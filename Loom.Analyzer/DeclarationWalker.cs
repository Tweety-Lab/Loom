using Loom.Analyzer.Symbols;
using Loom.Parser.AST;
using Loom.Parser.Rules.Default;

namespace Loom.Analyzer;

/// <summary>
/// Walks the AST and builds scoped <see cref="SymbolTable"/>s, defining symbols for all declarations.
/// </summary>
internal class DeclarationWalker : ASTVisitor
{
    /// <summary> The owning <see cref="AnalysisContext"/>. </summary>
    public AnalysisContext Context { get; private set; }

    /// <summary> The current <see cref="SymbolTable"/>. </summary>
    public SymbolTable CurrentTable { get; private set; }

    /// <summary> The current <see cref="Symbol"/>. </summary>
    public Symbol? CurrentSymbol { get; private set; }

    /// <summary> Initializes a new instance of the <see cref="DeclarationWalker"/> class. </summary>
    public DeclarationWalker(AnalysisContext context, SymbolTable currentTable)
    {
        Context = context;
        CurrentTable = currentTable;
    }

    [Visitor]
    public void Visit(ModuleNode node)
    {
        var symbol = new ModuleSymbol(node.Name.Token.Value);
        CurrentTable.Define(node.Name.Token.Value, symbol);

        WithScope(node, () => VisitChildren(node), symbol);
    }

    [Visitor]
    public void Visit(MethodDefinitionNode node)
    {
        var returnType = CurrentTable.Resolve(node.ReturnType.Value) as TypeSymbol ?? new TypeSymbol(node.ReturnType.Value, null);

        var symbol = new MethodDefinitionSymbol(node.MethodName.Token.Value, returnType);
        CurrentTable.Define(node.MethodName.Token.Value, symbol);

        WithScope(node, () => VisitChildren(node), symbol);
    }

    private void WithScope(ASTNode node, Action body, Symbol? symbol = null)
    {
        var parentTable = CurrentTable;
        var previousSymbol = CurrentSymbol;

        CurrentTable = new SymbolTable(parentTable);
        Context.SymbolTables[node] = CurrentTable;

        if (symbol is not null)
            CurrentSymbol = symbol;

        body();

        CurrentTable = parentTable;
        CurrentSymbol = previousSymbol;
    }

    /// <inheritdoc/>
    protected override void OnUnhandled(ASTNode node) => VisitChildren(node);
}
