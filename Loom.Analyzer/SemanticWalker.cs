using Loom.Analyzer.Symbols;
using Loom.Parser.AST;
using Loom.Parser.Rules.Default;

namespace Loom.Analyzer;

/// <summary>
/// Walks the AST and resolves scopes, tracking the current <see cref="SymbolTable"/> as it enters and exits declaration boundaries.
/// </summary>
internal class SemanticWalker : ASTVisitor
{
    /// <summary> Maps <see cref="ASTNode"/>s to their corresponding <see cref="SymbolTable"/>. </summary>
    public Dictionary<ASTNode, SymbolTable> SymbolTables { get; }

    /// <summary> The current <see cref="SymbolTable"/>. </summary>
    public SymbolTable CurrentTable { get; private set; } = null!;

    /// <summary> Initializes a new instance of the <see cref="SemanticWalker"/> class. </summary>
    public SemanticWalker(Dictionary<ASTNode, SymbolTable> symbolTables) => SymbolTables = symbolTables;

    public void SetRootTable(SymbolTable table) => CurrentTable = table;

    [Visitor]
    public void Visit(ModuleNode node)
    {
        var parent = CurrentTable;
        CurrentTable = SymbolTables[node];
        VisitChildren(node);
        CurrentTable = parent;
    }

    [Visitor]
    public void Visit(MethodDefinitionNode node)
    {
        var parent = CurrentTable;
        CurrentTable = SymbolTables[node];
        VisitChildren(node);
        CurrentTable = parent;
    }

    /// <inheritdoc/>
    protected override void OnUnhandled(ASTNode node) => VisitChildren(node);
}
