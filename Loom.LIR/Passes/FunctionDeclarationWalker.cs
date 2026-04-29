using Loom.Analyzer;
using Loom.Analyzer.Symbols;
using Loom.LIR.Builders;
using Loom.Parser.AST;
using Loom.Parser.Rules.Default;

namespace Loom.LIR.Passes;

internal class FunctionDeclarationWalker : ASTWalker
{
    private CompilationUnitBuilder unitBuilder;
    private AnalysisContext context;

    /// <summary> Initializes a new instance of the <see cref="FunctionDeclarationWalker"/> class. </summary>
    public FunctionDeclarationWalker(AnalysisContext context, CompilationUnitBuilder unitBuilder)
    {
        this.context = context;
        this.unitBuilder = unitBuilder;
    }

    [Visitor]
    public void Visit(MethodDeclarationNode node)
    {
        var method = context.ResolveSymbol(node).Symbol as MethodDefinitionSymbol;
        if (method == null) return;

        LIRType returnType = method.ReturnType.KnownType switch
        {
            TypeSymbol.DefaultType.Void => LIRType.Void,
            TypeSymbol.DefaultType.I32 => LIRType.Int32,
            _ => LIRType.Int32
        };

        var builder = unitBuilder.DefineFunction(method?.FullyQualifiedName ?? node.MethodName.Text, returnType, new List<LIRType>());

        if (node.Modifiers.Count > 0)
            builder.MetaData.Add("Modifiers", string.Join(", ", node.Modifiers.Select(m => m.Text)));

        builder.MetaData.Add("OwningType", "global");
    }
}
