using Loom.Analyzer.Symbols;
using Loom.Parser.AST;
using Loom.Parser.Rules.Default;
using System;
using System.Collections.Generic;
using System.Text;

namespace Loom.Analyzer;

internal class TypeWalker : ASTWalker
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
        WalkChildren(node);
    }
}
