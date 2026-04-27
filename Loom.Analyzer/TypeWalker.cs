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
    /// <summary> All currently mapped <see cref="ExpressionNode"/>s to their <see cref="TypeSymbol"/>. </summary>
    public Dictionary<ExpressionNode, TypeSymbol> ExpressionTypes { get; private set; }

    public AnalysisContext Context { get; private set; }

    /// <summary> Initializes a new instance of the <see cref="TypeWalker"/> class. </summary>
    public TypeWalker(Dictionary<ExpressionNode, TypeSymbol> expressionTypes, AnalysisContext context)
    {
        ExpressionTypes = expressionTypes;
        Context = context;
    }

    [Visitor]
    public void Visit(NumberLiteralNode node) => ExpressionTypes[node] = (TypeSymbol)Context.SymbolTables.First().Value.Resolve("i32")!;

    [Visitor]
    public void Visit(CallExpressionNode node)
    {
        if (Context.ResolveSymbol(node, node.MethodName) is MethodDefinitionSymbol methodSymbol)
            ExpressionTypes[node] = methodSymbol.ReturnType;
    }
}
