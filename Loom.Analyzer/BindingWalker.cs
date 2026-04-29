using Loom.Analyzer.Symbols;
using Loom.Parser.AST;
using Loom.Parser.Rules.Default;

namespace Loom.Analyzer;

/// <summary>
/// Walks the AST and binds <see cref="Symbol"/>s to <see cref="ASTNode"/>s.
/// </summary>
internal class BindingWalker : ASTWalker
{
    /// <summary> The owning <see cref="AnalysisContext"/>. </summary>
    public AnalysisContext Context { get; private set; }

    /// <summary> Initializes a new instance of the <see cref="DeclarationWalker"/> class. </summary>
    public BindingWalker(AnalysisContext context) => Context = context;

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
                    var moduleSymbol = Context.ResolveSymbol(import.ModuleName).Symbol as ModuleSymbol;
                    if (moduleSymbol == null)
                        continue;

                    var moduleNode = moduleSymbol.DeclaringNode;
                    if (moduleNode == null)
                        continue;

                    var exportedSymbol = Context.Binders[moduleNode].Lookup(node.BaseName)?.FirstOrDefault(s => s is MethodDefinitionSymbol m);

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