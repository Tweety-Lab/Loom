using Loom.Analyzer;
using Loom.Analyzer.Symbols;
using Loom.LIR.Builders;
using Loom.LIR.Generators;
using Loom.LIR.OpCodes;
using Loom.Parser.AST;
using Loom.Parser.Rules.Default;

namespace Loom.LIR.Passes;

internal class FunctionBodyWalker : ASTWalker
{
    private readonly Dictionary<Symbol, FunctionBuilder> functions;
    private readonly AnalysisContext context;

    private LIRGenerator? il;

    /// <summary> Initializes a new instance of the <see cref="FunctionBodyWalker"/> class. </summary>
    public FunctionBodyWalker(AnalysisContext context, Dictionary<Symbol, FunctionBuilder> functions)
    {
        this.context = context;
        this.functions = functions;
    }

    [Visitor]
    public void Visit(MethodDeclarationNode node)
    {
        var method = context.ResolveSymbol(node).Symbol as MethodDefinitionSymbol;
        if (method == null || !functions.TryGetValue(method, out var builder))
            return;

        il = builder.LIRGenerator;
    }

    [Visitor]
    public void Visit(CallExpressionNode node)
    {
        var method = context.ResolveSymbol(node.MethodName).Symbol as MethodDefinitionSymbol;
        if (method == null || !functions.TryGetValue(method, out var builder))
            return;

        var result = il!.Emit(LIROpCode.Call, builder.Build());
    }
}
