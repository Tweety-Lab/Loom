using Loom.Analyzer;
using Loom.Analyzer.Symbols;
using Loom.Common;
using Loom.LIR.Builders;
using Loom.LIR.Objects;
using Loom.LIR.Passes;
using Loom.Parser.AST;
using Loom.Parser.Rules.Default;

namespace Loom.LIR;

// This whole class is a hack

/// <summary>
/// A <see cref="ASTWalker"/> that converts the AST into Loom Intermediate Representation (LIR).
/// </summary>
internal class LIRASTWalker
{
    private readonly CompilationContext context;
    private readonly CompilationUnitBuilder unitBuilder = new();
    private readonly Dictionary<Symbol, FunctionBuilder> functions = new();

    public LIRASTWalker(CompilationContext context) => this.context = context;

    public LIRCompilationUnit Build(IEnumerable<ProgramNode> roots)
    {
        var rootList = roots.ToList();

        // Declare all functions
        var declPass = new FunctionDeclarationWalker(context.AnalysisContext, unitBuilder, functions);
        foreach (var root in rootList)
            declPass.Dispatch(root);

        // Emit bodies
        var bodyPass = new FunctionBodyWalker(context.AnalysisContext, functions);
        foreach (var root in rootList)
            bodyPass.Dispatch(root);

        return unitBuilder.Build();
    }
}
