using Loom.Analyzer.Analyzers;
using Loom.Analyzer.Symbols;
using Loom.Common.Reflection;
using Loom.Parser.AST;
using Loom.Parser.Rules;
using Loom.Parser.Rules.Default;
using System.Reflection;

namespace Loom.Analyzer;

public class AnalysisContext : ASTWalker
{
    /// <summary> Maps <see cref="ASTNode"/>s to their corresponding <see cref="SymbolTable"/>. </summary>
    public Dictionary<ASTNode, SymbolTable> SymbolTables { get; } = new();

    /// <summary> All registered analyzers the pipeline uses. </summary>
    public List<Analyzers.Analyzer> Analyzers
    {
        get
        {
            if (field != null)
                return field;

            field = LoomReflection.InstansiateAllWithAttribute<LoomAnalyzerAttribute>(Assembly.GetExecutingAssembly()).OfType<Analyzers.Analyzer>().ToList();

            return field;
        }
    }

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

        foreach (var analyzer in Analyzers)
        {
            analyzer.Context = this;
            root.Accept(analyzer);
        }
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
