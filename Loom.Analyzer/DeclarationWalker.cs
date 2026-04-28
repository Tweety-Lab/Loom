using Loom.Analyzer.Symbols;
using Loom.Parser.AST;
using Loom.Parser.Rules.Default;

namespace Loom.Analyzer;

/// <summary>
/// Walks the AST and builds scoped <see cref="Binder"/>s, defining symbols for all declarations.
/// </summary>
internal class DeclarationWalker : ASTVisitor
{
    /// <summary> The owning <see cref="AnalysisContext"/>. </summary>
    public AnalysisContext Context { get; private set; }

    /// <summary> The current <see cref="Binder"/>. </summary>
    public Binder CurrentTable { get; private set; }

    /// <summary> The current <see cref="Symbol"/>. </summary>
    public Symbol? CurrentSymbol { get; private set; }

    /// <summary> Initializes a new instance of the <see cref="DeclarationWalker"/> class. </summary>
    public DeclarationWalker(AnalysisContext context, Binder currentTable)
    {
        Context = context;
        CurrentTable = currentTable;
    }

    [Visitor]
    public void Visit(ModuleNode node)
    {
        var symbol = new ModuleSymbol(node.Name.BaseName);
        CurrentTable.Define(symbol);

        WithScope(node, () => VisitChildren(node), symbol);
    }

    [Visitor]
    public void Visit(MethodDefinitionNode node)
    {
        var returnType = CurrentTable.Lookup(node.ReturnType.Value) as TypeSymbol ?? new TypeSymbol(node.ReturnType.Value, null);

        var symbol = new MethodDefinitionSymbol(node.MethodName.BaseName, returnType);
        CurrentTable.Define(symbol);

        WithScope(node, () => VisitChildren(node), symbol);
    }

    private void WithScope(ASTNode node, Action body, Symbol? symbol = null)
    {
        var parentTable = CurrentTable;
        var previousSymbol = CurrentSymbol;

        CurrentTable = new Binder(parentTable);
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
