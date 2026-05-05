using Loom.Analyzer;
using Loom.Analyzer.Symbols;
using Loom.Common;
using Loom.LIR.Objects;
using Loom.Parser.AST;
using Loom.Parser.Rules.Default;

namespace Loom.LIR.Generation;

/// <summary>
/// Converts an Abstract Syntax Tree (AST) into Loom Intermediate Representation (LIR).
/// </summary>
public class ASTGenerator : ASTWalker
{
    private LIRCompilationUnit unit = null!;
    private CompilationContext context;

    /// <summary> Initializes a new instance of the <see cref="ASTGenerator"/> class. </summary>
    public ASTGenerator(CompilationContext context) => this.context = context;

    /// <summary> Generates Loom Intermediate Representation (LIR) from an Abstract Syntax Tree (AST) root. </summary>
    /// <param name="root"> The root of the AST. </param>
    /// <returns> The generated LIR unit. </returns>
    public LIRCompilationUnit Generate(ProgramNode root)
    {
        unit = new LIRCompilationUnit("Test");
        Dispatch(root);
        return unit;
    }

    [Visitor]
    public void VisitMethodDefinition(MethodDeclarationNode node)
    {
        MethodDefinitionSymbol? symbol = (MethodDefinitionSymbol?)context.AnalysisContext.GetSymbol(node).Symbol;

        if (symbol == null)
        {
            Console.WriteLine($"Could not find symbol for {node.MethodName}!");
            return;
        }

        LIRParameter[] parameters = new LIRParameter[symbol.Parameters.Count];
        for (int i = 0; i < parameters.Length; i++)
            parameters[i] = new LIRParameter(symbol.Parameters[i].Name, ConvertType(symbol.Parameters[i].Type));

        LIRFunction func = unit.DefineFunction(symbol.FullyQualifiedName, new LIRFunctionType(ConvertType(symbol.ReturnType), parameters));

        func.LIRGenerator.EmitReturn();
    }
    
    public LIRType ConvertType(TypeSymbol type) => type.KnownType switch
    {
        TypeSymbol.DefaultType.Void => LIRType.Void,
        TypeSymbol.DefaultType.Bool => LIRType.Boolean,
        TypeSymbol.DefaultType.I32 => LIRType.Int32,
        _ => LIRType.Int32, // Hack
    };
}
