using Loom.Analyzer.Symbols;
using Loom.Parser.AST;
using Loom.Parser.Rules.Default;

namespace Loom.Analyzer;

public class LoomAnalyzer : ASTWalker
{
    /// <summary> Maps <see cref="ASTNode"/>s to their corresponding <see cref="SymbolTable"/>. </summary>
    public Dictionary<ASTNode, SymbolTable> SymbolTables { get; } = new();

    private SymbolTable current = null!;

    /// <summary> Runs the given <see cref="ProgramNode"/> through the Semantic Analyzer. </summary>
    public void Analyse(ProgramNode root)
    {
        var rootTable = new SymbolTable();
        SymbolTables[root] = rootTable;

        var declWalker = new DeclarationWalker(SymbolTables, rootTable);
        declWalker.Visit(root);

        current = rootTable;
        root.Accept(this);
    }

    /// <inheritdoc/>
    public override void Visit(ModuleNode node)
    {
        var parentTable = current;
        current = SymbolTables[node];

        WalkChildren(node);

        current = parentTable;
    }
}
