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
    public Dictionary<ASTNode, Symbols.Binder> Binders { get; } = new();

    public Dictionary<ASTNode, Symbol?> BoundSymbols { get; } = new();

    /// <summary> Maps <see cref="ExpressionNode"/>s to their corresponding <see cref="TypeSymbol"/>. </summary>
    public Dictionary<ExpressionNode, TypeSymbol> ExpressionTypes { get; } = new();

    /// <summary> Maps <see cref="ASTNode"/>s to their parent <see cref="ASTNode"/>. </summary>
    public Dictionary<ASTNode, ASTNode> Parents { get; } = new();

    /// <summary> The entry point of the program or null if one could not be resolved. </summary>
    public MethodDefinitionSymbol? EntryPoint { get; private set; }

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

    /// <summary> Runs the given <see cref="ProgramNode"/>s through the Semantic Analyzer. </summary>
    public void Analyze(IEnumerable<ProgramNode> roots)
    {
        var rootList = roots.ToList();
        var rootTable = new Symbols.Binder();

        // Built in types
        rootTable.Define(new TypeSymbol("void", TypeSymbol.DefaultType.Void));
        rootTable.Define(new TypeSymbol("i32", TypeSymbol.DefaultType.I32));

        // Register all roots against the same root table
        foreach (var root in rootList)
            Binders[root] = rootTable;

        // Order here MATTERS

        // Resolve Declarations - across all trees first
        var declWalker = new DeclarationWalker(this, rootTable);
        foreach (var root in rootList)
            declWalker.Dispatch(root);

        // Resolve Parents
        var parentWalker = new ParentWalker(this);
        foreach (var root in rootList)
            parentWalker.Dispatch(root);

        // Resolve special binding
        var bindWalker = new BindingWalker(this);
        foreach (var root in rootList)
            bindWalker.Dispatch(root);

        // Resolve Types
        var typeWalker = new TypeWalker(this);
        foreach (var root in rootList)
            typeWalker.Dispatch(root);

        // Run analyzers
        foreach (var analyzer in Analyzers)
        {
            analyzer.Context = this;
            foreach (var root in rootList)
                root.Accept(analyzer);
        }

        // Resolve entry point
        EntryPoint = roots.SelectMany(r => r.Modules).SelectMany(m => Binders[m].Symbols).OfType<MethodDefinitionSymbol>().FirstOrDefault(s => string.Equals(s.Name, "main", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary> Gets the nearest <see cref="Binder"/> in scope for the given node. </summary>
    public Symbols.Binder? GetBinder(ASTNode node)
    {
        var current = node;
        while (current != null)
        {
            if (Binders.TryGetValue(current, out var binder))
                return binder;

            current = GetParent(current);
        }

        return null;
    }

    /// <summary> Gets the <see cref="Symbol"/> of the given <see cref="ASTNode"/>. </summary>
    public SymbolInfo GetSymbol(ASTNode node)
    {
        if (BoundSymbols.TryGetValue(node, out var symbol))
            return new SymbolInfo(symbol);

        return default;
    }
} 

