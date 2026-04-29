using Loom.Analyzer;
using Loom.Analyzer.Symbols;
using Loom.LIR.Builders;
using Loom.LIR.Generators;
using Loom.LIR.OpCodes;
using Loom.Parser.AST;
using Loom.Parser.Rules.Default;
using System.Diagnostics;

namespace Loom.LIR.Passes;

internal class FunctionBodyWalker : ASTWalker
{
    private AnalysisContext context;
    private CompilationUnitBuilder unitBuilder;

    private LIRGenerator? il;

    /// <summary> Initializes a new instance of the <see cref="FunctionBodyWalker"/> class. </summary>
    public FunctionBodyWalker(AnalysisContext context, CompilationUnitBuilder unitBuilder)
    {
        this.context = context;
        this.unitBuilder = unitBuilder;
    }

    [Visitor]
    public void Visit(MethodDeclarationNode node)
    {
        var method = (context.ResolveSymbol(node).Symbol as MethodDefinitionSymbol)!;

        FunctionBuilder func = unitBuilder.GetFunction(method.FullyQualifiedName);

        il = func.LIRGenerator;
    }

    [Visitor]
    public void Visit(CallExpressionNode node)
    {
        var method = (context.ResolveSymbol(node.MethodName).Symbol as MethodDefinitionSymbol)!;

        var builder = unitBuilder.GetFunction(method.FullyQualifiedName);

        var result = il!.Emit(LIROpCode.Call, builder.Build());
    }
}
