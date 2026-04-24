using Loom.Analyzer.Symbols;
using Loom.Parser.AST;
using Loom.Parser.Rules.Default;

namespace Loom.Analyzer;

internal class SemanticWalker : ASTWalker
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
        WalkChildren(node);
        CurrentTable = parent;
    }
}
