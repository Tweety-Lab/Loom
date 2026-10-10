using Loom.Analyzer.Analyzers;
using Loom.Analyzer.Symbols;
using Loom.Common.Diagnostics;
using Loom.Common.Reflection;
using Loom.Parser.AST;
using Loom.Parser.AST.Rules.Default;
using System.Reflection;

namespace Loom.Analyzer;

public class AnalysisContext
{
    /// <summary> The <see cref="Common.Diagnostics.DiagnosticContext"/> this reports to, if any. </summary>
    public DiagnosticContext? DiagnosticContext { get; set; }
    
    /// <summary> Maps <see cref="ASTNode"/>s to their corresponding <see cref="Binder"/>. </summary>
    public Dictionary<ASTNode, Symbols.Binder> Binders { get; } = new();
    
    /// <summary> Maps <see cref="ASTNode"/>s to the <see cref="SymbolInfo"/> bound to them. </summary>
    public Dictionary<ASTNode, SymbolInfo> Symbols { get; } = new();

    /// <summary> Maps <see cref="ExpressionNode"/>s to their corresponding <see cref="TypeSymbol"/>. </summary>
    public Dictionary<ExpressionNode, TypeSymbol> ExpressionTypes { get; } = new();

    /// <summary> Maps <see cref="ASTNode"/>s to their parent <see cref="ASTNode"/>. </summary>
    public Dictionary<ASTNode, ASTNode> Parents { get; } = new();

    /// <summary> The symbols whose ownership was transferred away before the end of the method declaring them. </summary>
    public HashSet<Symbol> MovedValues { get; } = new();

    /// <summary> The entry point of the program or null if one could not be resolved. </summary>
    public MethodSymbol? EntryPoint { get; private set; }

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
    /// <remarks> <typeparamref name="T"/> may be an interface, e.g. <see cref="ITypeDeclarationNode"/>. </remarks>
    public T? FirstAncestorOrSelf<T>(ASTNode node) where T : class => node as T ?? FirstAncestor<T>(node);

    /// <summary> Gets the first parent of the given type, excluding the node itself. </summary>
    public T? FirstAncestor<T>(ASTNode node) where T : class
    {
        var current = GetParent(node);
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
        rootTable.Define(new TypeSymbol("i32", TypeSymbol.DefaultType.I32) { IsValueType = true });
        rootTable.Define(new TypeSymbol("i64", TypeSymbol.DefaultType.I64) { IsValueType = true });
        rootTable.Define(new TypeSymbol("iptr", TypeSymbol.DefaultType.IPtr) { IsValueType = true });
        rootTable.Define(new TypeSymbol("bool", TypeSymbol.DefaultType.Bool) { IsValueType = true });
        rootTable.Define(new TypeSymbol("char", TypeSymbol.DefaultType.Char) { IsValueType = true });

        // Register all roots against the same root table
        foreach (var root in rootList)
            Binders[root] = rootTable;

        // Order here MATTERS

        // Resolve Declarations
        var declWalker = new DeclarationWalker(this, rootTable);
        foreach (var root in rootList)
            declWalker.Dispatch(root);

        // Resolve Parents
        var parentWalker = new ParentWalker(this);
        foreach (var root in rootList)
            parentWalker.Dispatch(root);

        // Bind identifiers and resolve declared types
        var bindWalker = new BindingWalker(this);
        foreach (var root in rootList)
            bindWalker.Dispatch(root);

        // Resolve Types
        var typeWalker = new TypeWalker(this);
        foreach (var root in rootList)
            typeWalker.Dispatch(root);

        // Resolve Ownership flow
        var ownershipWalker = new OwnershipWalker(this);
        foreach (var root in rootList)
            ownershipWalker.Dispatch(root);

        // Run analyzers
        foreach (var analyzer in Analyzers)
        {
            analyzer.Context = this;
            foreach (var root in rootList)
                root.Accept(analyzer);
        }

        // Resolve entry point
        EntryPoint = roots.SelectMany(r => r.Modules).SelectMany(m => Binders[m].Symbols).OfType<MethodSymbol>().FirstOrDefault(s => string.Equals(s.Name, "main", StringComparison.OrdinalIgnoreCase));
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

    /// <summary> Gets the <see cref="SymbolInfo"/> bound to <paramref name="node"/>. </summary>
    public SymbolInfo GetSymbol(ASTNode node)
    {
        if (Symbols.TryGetValue(node, out var info))
            return info;

        if (node is MemberAccessExpressionNode member)
        {
            var receiverType = ExpressionTypes.TryGetValue(member.Receiver, out var receiver) ? receiver : GetSymbol(member.Receiver).Symbol as TypeSymbol;
            return SymbolInfo.OfCandidates(receiverType?.Members.Where(m => m.Name == member.Name.BaseName) ?? []);
        }

        return SymbolInfo.None;
    }

    /// <summary> Binds <paramref name="node"/> to the single <paramref name="symbol"/> its name declares. </summary>
    public void Bind(ASTNode node, Symbol symbol) => Symbols[node] = SymbolInfo.Of(symbol);

    /// <summary> Binds <paramref name="node"/> to every <paramref name="candidates"/> its name could refer to. </summary>
    public void Bind(ASTNode node, SymbolInfo symbols) => Symbols[node] = symbols;
}
