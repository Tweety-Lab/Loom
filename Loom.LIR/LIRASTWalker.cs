using Loom.Analyzer;
using Loom.Common;
using Loom.LIR.Builders;
using Loom.LIR.Generators;
using Loom.LIR.OpCodes;
using Loom.Parser.AST;
using Loom.Parser.Rules.Default;

namespace Loom.LIR;

// This whole class is a hack

/// <summary>
/// A <see cref="ASTWalker"/> that converts the AST into Loom Intermediate Representation (LIR).
/// </summary>
internal class LIRASTWalker : ASTWalker
{
    private readonly CompilationUnitBuilder unitBuilder = new();
    private FunctionBuilder? currentFunction;
    private LIRGenerator? il;

    private CompilationContext context;

    /// <summary> Initializes a new instance of the <see cref="LIRASTWalker"/> class. </summary>
    public LIRASTWalker(CompilationContext context) => this.context = context;

    /// <summary> Builds the root of the AST into an <see cref="LIRCompilationUnit"/>. </summary>
    public LIRCompilationUnit Build(ASTNode root)
    {
        Dispatch(root);
        return unitBuilder.Build();
    }

    [Visitor]
    public void Visit(MethodDeclarationNode node)
    {
        string fullName = context.AnalysisContext.FirstAncestorOrSelf<ModuleNode>(node)?.Name.Text + "::" + node.MethodName.Text;
        currentFunction = unitBuilder.DefineFunction(fullName, LIRType.Int32, new List<LIRType>());

        il = currentFunction.LIRGenerator;
    }

    [Visitor]
    public void Visit(CallExpressionNode node)
    {
        il.Emit(LIROpCode.Call, new LIRFunctionValue(new LIRFunction(node.MethodName.BaseName, new LIRFunctionType(LIRType.Void, new List<LIRType>()), new List<LIRBasicBlock>())));
    }
}
