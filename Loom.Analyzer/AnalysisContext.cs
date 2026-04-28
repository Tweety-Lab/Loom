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

    /// <summary> Maps <see cref="ASTNode"/>s to their corresponding <see cref="Binder"/>. </summary>
    public Dictionary<ASTNode, Symbols.Binder> SymbolTables { get; } = new();

    /// <summary> Maps <see cref="ExpressionNode"/>s to their corresponding <see cref="TypeSymbol"/>. </summary>
    public Dictionary<ExpressionNode, TypeSymbol> ExpressionTypes { get; } = new();

    /// <summary> Maps <see cref="ASTNode"/>s to their parent <see cref="ASTNode"/>. </summary>
    public Dictionary<ASTNode, ASTNode> Parents { get; } = new();

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

    /// <summary> Gets the first parent (or self) of the given type. </summary>
    public T? FirstAncestorOrSelf<T>(ASTNode node) where T : ASTNode
    {
        var current = node;
        while (current != null)
        {
            if (current is T match)
                return match;

            current = GetParent(current);
        }
        return null;
    }

    /// <summary> Gets the immediate parent of the given <see cref="ASTNode"/>. </summary>
    public ASTNode? GetParent(ASTNode node) => Parents.TryGetValue(node, out var parent) ? parent : null;

    /// <summary> Runs the given <see cref="ProgramNode"/> through the Semantic Analyzer. </summary>
    public void Analyze(ProgramNode root)
    {
        var rootTable = new Symbols.Binder();

        // Built-in types
        rootTable.Define(new TypeSymbol("void", TypeSymbol.Type.Void));
        rootTable.Define(new TypeSymbol("i32", TypeSymbol.Type.I32));

        SymbolTables[root] = rootTable;

        // Order here MATTERS

        // Resolve Declarations
        var declWalker = new DeclarationWalker(this, rootTable);
        declWalker.VisitChildren(root);

        // Resolve Parents
        var parentWalker = new ParentWalker(this);
        parentWalker.VisitChildren(root);

        // Resolve Scopes
        var semWalker = new SemanticWalker(this);
        semWalker.SetRootTable(rootTable);
        semWalker.VisitChildren(root);


        // Resolve Types
        var typeWalker = new TypeWalker(this);
        typeWalker.Dispatch(root);

        foreach (var analyzer in Analyzers)
        {
            analyzer.Context = this;
            root.Accept(analyzer);
        }
    }

    /// <summary> Resolves a <see cref="Symbol"/> by name or null if not found. </summary>
    public Symbol? ResolveSymbol(ASTNode node, string name)
    {
        var current = node;
        while (current != null)
        {
            if (SymbolTables.TryGetValue(current, out var table))
                return table.Lookup(name);

            current = GetParent(current);
        }

        return null;
    }
}
