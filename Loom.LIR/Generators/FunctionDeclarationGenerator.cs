using Loom.Analyzer;
using Loom.Analyzer.Symbols;
using Loom.LIR.Builders;
using Loom.Parser.AST;
using Loom.Parser.Rules.Default;

namespace Loom.LIR.Passes;

internal class FunctionDeclarationGenerator : ASTWalker
{
    /// <summary> The owning <see cref="AnalysisContext"/>. </summary>
    public AnalysisContext Context { get; }

    /// <summary> The owning <see cref="CompilationUnitBuilder"/>. </summary>
    public CompilationUnitBuilder UnitBuilder { get; }

    public LIRSemanticContext SemanticContext { get; }

    /// <summary> Initializes a new instance of the <see cref="FunctionDeclarationGenerator"/> class. </summary>
    public FunctionDeclarationGenerator(AnalysisContext context, CompilationUnitBuilder unitBuilder, LIRSemanticContext semanticContext)
    {
        Context = context;
        UnitBuilder = unitBuilder;
        SemanticContext = semanticContext;
    }

    [Visitor]
    public void Visit(MethodDeclarationNode node)
    {
        var method = (Context.ResolveSymbol(node).Symbol as MethodDefinitionSymbol)!;

        LIRType returnType = method.ReturnType.KnownType switch
        {
            TypeSymbol.DefaultType.Void => LIRType.Void,
            TypeSymbol.DefaultType.I32 => LIRType.Int32,
            _ => LIRType.Int32
        };

        var builder = UnitBuilder.DefineFunction(method?.FullyQualifiedName ?? node.MethodName.Text, returnType, new List<LIRType>());

        if (node.Modifiers.Count > 0)
            builder.MetaData.Add("Modifiers", string.Join(", ", node.Modifiers.Select(m => m.Text)));

        builder.MetaData.Add("OwningType", "global");

        SemanticContext.Functions.Add(method, builder);
    }
}
