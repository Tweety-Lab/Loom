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
        current = new();
        SymbolTables[root] = current;

        root.Accept(this);
    }

    /// <inheritdoc/>
    public override void Visit(ModuleNode node)
    {
        SymbolTables[node] = current;
        current.Define(node.Name, new ModuleSymbol(node.Name));

        base.Visit(node);
    }

    /// <inheritdoc/>
    public override void Visit(BlockNode node)
    {
        var parent = current;
        current = new SymbolTable(current);
        SymbolTables[node] = current;

        base.Visit(node);

        current = parent;
    }
}
