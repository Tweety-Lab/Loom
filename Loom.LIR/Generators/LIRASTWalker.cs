using Loom.Analyzer;
using Loom.Analyzer.Symbols;
using Loom.Common;
using Loom.LIR.Objects;
using Loom.LIR.Passes;
using Loom.Parser.AST;
using Loom.Parser.Rules.Default;

namespace Loom.LIR.Generators;

/// <summary>
/// A <see cref="ASTWalker"/> that converts the AST into Loom Intermediate Representation (LIR).
/// </summary>
internal class LIRASTWalker
{
    public LIRSemanticContext SemanticContext { get; private set; } = new();

    private CompilationContext context;
    private LIRCompilationUnit unitBuilder = new(new());

    /// <summary> Initializes a new instance of the <see cref="LIRASTWalker"/> class. </summary>
    public LIRASTWalker(CompilationContext context) => this.context = context;

    public LIRCompilationUnit Build(IEnumerable<ProgramNode> roots)
    {
        var rootList = roots.ToList();

        // Declare all functions
        var declPass = new FunctionDeclarationGenerator(context.AnalysisContext, unitBuilder, SemanticContext);
        foreach (var root in rootList)
            declPass.Dispatch(root);

        // Emit bodies
        var bodyPass = new FunctionBodyGenerator(context.AnalysisContext, unitBuilder, SemanticContext);
        foreach (var root in rootList)
            bodyPass.Dispatch(root);

        return unitBuilder;
    }
}
