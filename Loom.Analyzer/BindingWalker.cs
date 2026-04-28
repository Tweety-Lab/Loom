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
    public void Visit(ImportNode node)
    {
        var binder = Context.Binders.First().Value;
        var symbol = binder.Lookup(node.ModuleName.BaseName)?.First() as ModuleSymbol;

        Context.BoundSymbols[node.ModuleName] = symbol;
    }

    [Visitor]
    public void Visit(CallExpressionNode node)
    {
        var binder = Context.FirstAncestorOrSelf<MethodDefinitionNode>(node) is { } method ? Context.Binders[method] : Context.Binders.First().Value;
        var methodSymbol = binder.Lookup(node.MethodName.BaseName)?.First() as MethodDefinitionSymbol;
        Context.BoundSymbols[node.MethodName] = methodSymbol;
    }
}

