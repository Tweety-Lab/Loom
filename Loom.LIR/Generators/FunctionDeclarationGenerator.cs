using Loom.Analyzer;
using Loom.Analyzer.Symbols;
using Loom.LIR.Objects;
using Loom.Parser.AST;
using Loom.Parser.Rules.Default;

namespace Loom.LIR.Passes;

internal class FunctionDeclarationGenerator : ASTWalker
{
    /// <summary> The owning <see cref="AnalysisContext"/>. </summary>
    public AnalysisContext Context { get; }

    /// <summary> The owning <see cref="LIRCompilationUnit"/>. </summary>
    public LIRCompilationUnit CompilationUnit { get; }

    public LIRSemanticContext SemanticContext { get; }

    /// <summary> Initializes a new instance of the <see cref="FunctionDeclarationGenerator"/> class. </summary>
    public FunctionDeclarationGenerator(AnalysisContext context, LIRCompilationUnit compilationUnit, LIRSemanticContext semanticContext)
    {
        Context = context;
        CompilationUnit = compilationUnit;
        SemanticContext = semanticContext;
    }

    [Visitor]
    public void Visit(MethodDeclarationNode node)
    {
        var method = (Context.GetSymbol(node).Symbol as MethodDefinitionSymbol)!;

        LIRType returnType = method.ReturnType.KnownType switch
        {
            TypeSymbol.DefaultType.Void => LIRType.Void,
            TypeSymbol.DefaultType.I32 => LIRType.Int32,
            _ => LIRType.Int32
        };

        List<LIRParameter> parameters = method.Parameters.Select(p => new LIRParameter(p.Name, MapType(p.Type.KnownType ?? TypeSymbol.DefaultType.I32))).ToList();

        var builder = CompilationUnit.DefineFunction(method?.FullyQualifiedName ?? node.MethodName.Text, new LIRFunctionType(returnType, parameters));

        if (node.Modifiers.Count > 0)
            builder.MetaData.Add("Modifiers", string.Join(", ", node.Modifiers.Select(m => m.Text)));

        builder.MetaData.Add("OwningType", "global");

        SemanticContext.Functions.Add(method, builder);
    }


    private static LIRType MapType(TypeSymbol.DefaultType type) => type switch
    {
        TypeSymbol.DefaultType.Void => LIRType.Void,
        TypeSymbol.DefaultType.I32 => LIRType.Int32,
        _ => LIRType.Int32
    };
}
