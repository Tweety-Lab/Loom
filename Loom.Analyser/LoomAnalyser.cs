using Loom.Analyser.Symbols;
using Loom.Parser.AST;
using Loom.Parser.Rules.Default;

namespace Loom.Analyser;

public class LoomAnalyser : ASTWalker
{
    /// <summary> Maps <see cref="ASTNode"/>s to their corresponding <see cref="SymbolTable"/>. </summary>
    public Dictionary<ASTNode, SymbolTable> SymbolTables { get; } = new();

    private SymbolTable current = null!;

    /// <summary> Runs the given <see cref="ProgramNode"/> through the Semantic Analyser. </summary>
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
