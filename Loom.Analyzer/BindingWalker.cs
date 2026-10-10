using Loom.Analyzer.Symbols;
using Loom.Parser.AST;
using Loom.Parser.AST.Rules.Default;

namespace Loom.Analyzer;

/// <summary>
/// Walks the AST and binds <see cref="Symbol"/>s to <see cref="ASTNode"/>s, resolving declared types
/// onto their symbols and binding identifier references to their symbols.
/// </summary>
internal class BindingWalker : ASTWalker
{
    /// <summary> The owning <see cref="AnalysisContext"/>. </summary>
    public AnalysisContext Context { get; private set; }

    /// <summary> Initializes a new instance of the <see cref="BindingWalker"/> class. </summary>
    public BindingWalker(AnalysisContext context) => Context = context;

    [Visitor]
    public void Visit(MethodDeclarationNode node)
    {
        if (Context.GetSymbol(node).Symbol is MethodSymbol symbol)
            symbol.ReturnType = TypeResolver.Resolve(Context, node, node.ReturnType);
    }

    [Visitor]
    public void Visit(ParameterNode node)
    {
        if (Context.GetSymbol(node).Symbol is ParameterSymbol symbol)
            symbol.Type = TypeResolver.Resolve(Context, node, node.Type);
    }

    [Visitor]
    public void Visit(FieldDeclarationNode node)
    {
        if (Context.GetSymbol(node).Symbol is FieldSymbol symbol)
            symbol.Type = TypeResolver.Resolve(Context, node, node.Variable.Type);
    }

    [Visitor]
    public void Visit(LocalDeclarationStatementNode node)
    {
        if (Context.GetSymbol(node).Symbol is LocalVariableSymbol symbol)
            symbol.Type = TypeResolver.Resolve(Context, node, node.Variable.Type);
    }

    [Visitor]
    public void Visit(IdentifierNameNode node)
    {
        // A name may be declared more than once when it is overloaded. Every declaration is bound as a candidate so
        // that overload resolution can pick between them once the types of the arguments are known.
        Context.Bind(node, SymbolInfo.OfCandidates(Lookup(node)));
    }

    /// <summary> Resolves every <see cref="Symbol"/> the name of <paramref name="node"/> could refer to. </summary>
    private IEnumerable<Symbol> Lookup(IdentifierNameNode node)
    {
        // Search local scope
        if (Context.GetBinder(node)?.Lookup(node.BaseName) is { } local)
            return local;

        // Search imported modules
        var programNode = Context.FirstAncestorOrSelf<ProgramNode>(node);

        if (programNode == null)
            return [];

        foreach (var import in programNode.Imports)
        {
            var moduleSymbol = Context.GetSymbol(import.ModuleName).Symbol as ModuleSymbol;
            if (moduleSymbol?.DeclaringNode is not ModuleNode moduleNode)
                continue;

            // The first module that declares the name owns it, so that an import cannot be shadowed by a later one.
            if (Context.Binders[moduleNode].Lookup(node.BaseName) is { } imported)
                return imported.Where(s => s is not ModuleSymbol);
        }

        return [];
    }
}
