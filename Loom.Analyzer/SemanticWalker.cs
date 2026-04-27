using Loom.Analyzer.Symbols;
using Loom.Parser.AST;
using Loom.Parser.Rules.Default;

namespace Loom.Analyzer;

/// <summary>
/// Walks the AST and resolves scopes, tracking the current <see cref="SymbolTable"/> as it enters and exits declaration boundaries.
/// </summary>
internal class SemanticWalker : ASTVisitor
{
    /// <summary> The owning <see cref="AnalysisContext"/>. </summary>
    public AnalysisContext Context { get; private set; }

    /// <summary> The current <see cref="SymbolTable"/>. </summary>
    public SymbolTable CurrentTable { get; private set; } = null!;

    /// <summary> Initializes a new instance of the <see cref="SemanticWalker"/> class. </summary>
    public SemanticWalker(AnalysisContext context) => Context = context;

    public void SetRootTable(SymbolTable table) => CurrentTable = table;

    [Visitor]
    public void Visit(ModuleNode node)
    {
        var parent = CurrentTable;
        CurrentTable = Context.SymbolTables[node];
        VisitChildren(node);
        CurrentTable = parent;
    }

    [Visitor]
    public void Visit(MethodDefinitionNode node)
    {
        var parent = CurrentTable;
        CurrentTable = Context.SymbolTables[node];
        VisitChildren(node);
        CurrentTable = parent;
    }

    /// <inheritdoc/>
    protected override void OnUnhandled(ASTNode node) => VisitChildren(node);
}
