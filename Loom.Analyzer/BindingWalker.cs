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
        // Search local scope
        var symbol = Context.GetBinder(node)?.Lookup(node.BaseName)?.First();

        // Search imported modules
        if (symbol == null)
        {
            var programNode = Context.FirstAncestorOrSelf<ProgramNode>(node);
            if (programNode != null)
            {
                foreach (var import in programNode.Imports)
                {
                    var moduleSymbol = Context.GetSymbol(import.ModuleName).Symbol as ModuleSymbol;
                    if (moduleSymbol?.DeclaringNode is not ModuleNode moduleNode)
                        continue;

                    var exportedSymbol = Context.Binders[moduleNode].Lookup(node.BaseName)?.FirstOrDefault(s => s is not ModuleSymbol);

                    if (exportedSymbol != null)
                    {
                        symbol = exportedSymbol;
                        break;
                    }
                }
            }
        }

        Context.BoundSymbols[node] = symbol;
    }
}
