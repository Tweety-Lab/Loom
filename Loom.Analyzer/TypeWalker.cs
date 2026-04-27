using Loom.Analyzer.Symbols;
using Loom.Parser.AST;
using Loom.Parser.Rules.Default;

namespace Loom.Analyzer;

/// <summary>
/// Walks the AST and resolves the <see cref="TypeSymbol"/> for every <see cref="ExpressionNode"/>.
/// </summary>
internal class TypeWalker : ASTVisitor
{
    /// <summary> All currently mapped <see cref="ExpressionNode"/>s to their <see cref="TypeSymbol"/>. </summary>
    public Dictionary<ExpressionNode, TypeSymbol> ExpressionTypes { get; private set; }

    /// <summary> The root <see cref="Symbol"/>. </summary>
    public SymbolTable RootTable { get; private set; }

    /// <summary> Initializes a new instance of the <see cref="TypeWalker"/> class. </summary>
    public TypeWalker(Dictionary<ExpressionNode, TypeSymbol> expressionTypes, SymbolTable rootTable) => (ExpressionTypes, RootTable) = (expressionTypes, rootTable);

    [Visitor]
    public void Visit(NumberLiteralNode node)
    {
        ExpressionTypes[node] = (TypeSymbol)RootTable.Resolve("i32")!;
        VisitChildren(node);
    }

    /// <inheritdoc/>
    protected override void OnUnhandled(ASTNode node) => VisitChildren(node);
}
