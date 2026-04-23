using Loom.Analyser.Symbols;
using Loom.Parser.AST;
using Loom.Parser.Rules.Default;

namespace Loom.Analyser;

public class LoomAnalyser
{
    /// <summary> Maps <see cref="ASTNode"/>s to their corresponding <see cref="SymbolTable"/>. </summary>
    public Dictionary<ASTNode, SymbolTable> SymbolTables { get; } = new();

    /// <summary> Runs the given <see cref="ProgramNode"/> through the Semantic Analyser. </summary>
    public void Analyse(ProgramNode root)
    {
        var globalTable = new SymbolTable();
        SymbolTables[root] = globalTable;
    }
}
