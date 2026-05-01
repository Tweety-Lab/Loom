using Loom.Analyzer.Symbols;
using Loom.Parser.AST;
using Loom.Parser.Rules.Default;

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
    public void Visit(NumberLiteralNode node) => Context.ExpressionTypes[node] = (TypeSymbol)Context.Binders.First().Value.Lookup("i32")!.First();

    [Visitor]
    public void Visit(IdentifierNameNode node)
    {
        var symbol = Context.GetSymbol(node).Symbol;

        var type = symbol switch
        {
            LocalVariableSymbol local => local.Type,
            MethodDefinitionSymbol method => method.ReturnType,
            _ => null
        };

        if (type != null)
            Context.ExpressionTypes[node] = type;
    }

    [Visitor]
    public void Visit(CallExpressionNode node)
    {
        if (Context.GetSymbol(node.MethodName).Symbol is MethodDefinitionSymbol methodSymbol)
            Context.ExpressionTypes[node] = methodSymbol.ReturnType;
    }
}
