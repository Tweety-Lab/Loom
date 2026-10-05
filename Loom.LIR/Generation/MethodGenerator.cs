using Loom.Analyzer;
using Loom.Analyzer.Symbols;
using Loom.Common;
using Loom.LIR.Objects;
using Loom.Parser.AST;
using Loom.Parser.AST.Rules.Default;

namespace Loom.LIR.Generation;

/// <summary>
/// Generates the Loom Intermediate Representation (LIR) of the methods declared outside of a type.
/// </summary>
internal class MethodGenerator
{
    private CompilationContext context;
    private LIRCompilationUnit unit;

    /// <summary> Initializes a new instance of the <see cref="MethodGenerator"/> class. </summary>
    public MethodGenerator(CompilationContext context, LIRCompilationUnit unit)
    {
        this.context = context;
        this.unit = unit;
    }

    public void GenerateMethodDeclaration(MethodDeclarationNode node)
    {
        MethodSymbol? symbol = (MethodSymbol?)context.AnalysisContext.GetSymbol(node).Symbol;

        if (symbol == null)
        {
            Console.WriteLine($"Could not find symbol for {node.MethodName}!");
            return;
        }

        if (symbol.IsGeneric)
            return;

        var funcType = ASTGenerator.BuildFunctionType(symbol);

        bool isExtern = node.HasModifier(Parser.Tokenizer.Token.TokenType.Extern);

        unit.DefineFunction(symbol.FullyQualifiedName, funcType, !isExtern);
    }

    public void GenerateMethodBody(MethodDeclarationNode node)
    {
        MethodSymbol? symbol = (MethodSymbol?)context.AnalysisContext.GetSymbol(node).Symbol;

        if (symbol == null)
        {
            Console.WriteLine($"Could not find symbol for {node.MethodName}!");
            return;
        }

        if (symbol.IsGeneric)
            return;

        if (symbol.IsExtern)
            return;

        LIRFunction func = unit.GetFunction(symbol.FullyQualifiedName) ?? throw new Exception($"Could not find function {symbol.FullyQualifiedName}!");
        StatementGenerator statementGen = new StatementGenerator(context, unit, func);

        foreach (var content in node.Body?.Contents ?? Enumerable.Empty<ASTNode>())
            if (content is StatementNode statementNode)
                statementGen.EmitStatement(statementNode);

        if (func.Type.ReturnType == LIRType.Void && func.LIRGenerator!.WritingBlock.Terminator == null)
            func.LIRGenerator.EmitReturn();
    }
}