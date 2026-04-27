using Loom.Analyzer.Symbols;
using Loom.Parser.AST;
using Loom.Parser.Rules.Default;
using System.Diagnostics;

namespace Loom.Analyzer;

/// <summary>
/// Walks the AST and resolves the <see cref="TypeSymbol"/> for every <see cref="ExpressionNode"/>.
/// </summary>
internal class TypeWalker : ASTWalker
{
    /// <summary> The owning <see cref="AnalysisContext"/>. </summary>
    public AnalysisContext Context { get; private set; }

    /// <summary> Initializes a new instance of the <see cref="TypeWalker"/> class. </summary>
    public TypeWalker(AnalysisContext context) => Context = context;

    [Visitor]
    public void Visit(NumberLiteralNode node) => Context.ExpressionTypes[node] = (TypeSymbol)Context.SymbolTables.First().Value.Resolve("i32")!;

    [Visitor]
    public void Visit(CallExpressionNode node)
    {
        if (Context.ResolveSymbol(node, node.MethodName) is MethodDefinitionSymbol methodSymbol)
            Context.ExpressionTypes[node] = methodSymbol.ReturnType;
    }
}
