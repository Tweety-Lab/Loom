using Loom.Analyzer.Analyzers;
using Loom.Analyzer.Symbols;
using Loom.Common.Diagnostics;
using Loom.Common.Reflection;
using Loom.Parser.AST;
using Loom.Parser.Rules.Default;
using System.Reflection;

namespace Loom.Analyzer;

public class AnalysisContext
{
    /// <summary> The <see cref="Common.Diagnostics.DiagnosticContext"/> this reports to, if any. </summary>
    public DiagnosticContext? DiagnosticContext { get; set; }

    /// <summary> Maps <see cref="ASTNode"/>s to their corresponding <see cref="SymbolTable"/>. </summary>
    public Dictionary<ASTNode, SymbolTable> SymbolTables { get; } = new();

    /// <summary> Maps <see cref="ExpressionNode"/>s to their corresponding <see cref="TypeSymbol"/>. </summary>
    public Dictionary<ExpressionNode, TypeSymbol> ExpressionTypes { get; } = new();

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

    /// <summary> Initializes a new instance of the <see cref="AnalysisContext"/> class. </summary>
    public AnalysisContext(DiagnosticContext? diagnosticContext = null) => DiagnosticContext = diagnosticContext;

    /// <summary> Runs the given <see cref="ProgramNode"/> through the Semantic Analyzer. </summary>
    public void Analyze(ProgramNode root)
    {
        var rootTable = new SymbolTable();

        // Built-in types
        rootTable.Define("void", new TypeSymbol("void", TypeSymbol.Type.Void));
        rootTable.Define("i32", new TypeSymbol("i32", TypeSymbol.Type.I32));

        SymbolTables[root] = rootTable;

        // Resolve Declarations
        var declWalker = new DeclarationWalker(SymbolTables, rootTable);
        declWalker.WalkChildren(root);

        // Resolve Types
        var typeWalker = new TypeWalker(ExpressionTypes, rootTable);
        typeWalker.WalkChildren(root);


        var semWalker = new SemanticWalker(SymbolTables);
        semWalker.SetRootTable(rootTable);
        semWalker.WalkChildren(root);

        foreach (var analyzer in Analyzers)
        {
            analyzer.Context = this;
            root.Accept(analyzer);
        }
    }

    /// <summary> Resolves a <see cref="Symbol"/> by name or null if not found. </summary>
    public Symbol? ResolveSymbol(ASTNode node, string name)
    {
        if (!SymbolTables.TryGetValue(node, out var table))
            return null;

        return table.Resolve(name);
    }
}
