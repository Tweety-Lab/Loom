using Loom.Analyzer;
using Loom.Analyzer.Symbols;
using Loom.Common;
using Loom.LIR.Objects;
using Loom.Parser.Rules.Default;

namespace Loom.LIR.Generation;

/// <summary>
/// Converts an Abstract Syntax Tree (AST) into Loom Intermediate Representation (LIR).
/// </summary>
public class ASTGenerator
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

        // Declare structs
        foreach (var module in root.Modules)
            foreach (var content in module.Body.Contents)
                if (content is StructDeclarationNode structDeclaration)
                    GenerateStructDeclaration(structDeclaration);

        // Declare methods
        foreach (var module in root.Modules)
            foreach (var content in module.Body.Contents)
                if (content is MethodDeclarationNode method)
                    GenerateMethodDeclaration(method);

        // Define method bodies
        foreach (var module in root.Modules)
            foreach (var content in module.Body.Contents)
                if (content is MethodDeclarationNode method)
                    GenerateMethodBody(method);

        return unit;
    }

    public void GenerateStructDeclaration(StructDeclarationNode node)
    {
        TypeSymbol? symbol = (TypeSymbol?)context.AnalysisContext.GetSymbol(node).Symbol;

        if (symbol == null)
        {
            Console.WriteLine($"Could not find symbol for {node.StructName}!");
            return;
        }

        LIRStruct structObj = unit.DefineStruct(symbol.FullyQualifiedName);

        foreach (var content in node.Body.Contents)
            if (content is MethodDeclarationNode method)
                GenerateStructMethod(structObj, method);
    }

    public void GenerateStructMethod(LIRStruct structObj, MethodDeclarationNode node)
    {
        MethodDefinitionSymbol? symbol = (MethodDefinitionSymbol?)context.AnalysisContext.GetSymbol(node).Symbol;

        if (symbol == null)
        {
            Console.WriteLine($"Could not find symbol for {node.MethodName}!");
            return;
        }

        var funcType = BuildFunctionType(symbol);

        bool isExtern = node.Modifiers.Any(m => m.Type == Parser.Tokenizer.Token.TokenType.Extern);

        if (isExtern)
        {
            structObj.DeclareMethod(symbol.FullyQualifiedName, funcType);
            return;
        }

        LIRFunction func = structObj.DefineMethod(symbol.FullyQualifiedName, funcType);
        StatementGenerator statementGen = new StatementGenerator(context, unit, func);

        foreach (var content in node.Body.Contents)
            if (content is StatementNode statementNode)
                statementGen.EmitStatement(statementNode);
    }

    public void GenerateMethodDeclaration(MethodDeclarationNode node)
    {
        MethodDefinitionSymbol? symbol = (MethodDefinitionSymbol?)context.AnalysisContext.GetSymbol(node).Symbol;

        if (symbol == null)
        {
            Console.WriteLine($"Could not find symbol for {node.MethodName}!");
            return;
        }

        var funcType = BuildFunctionType(symbol);

        bool isExtern = node.Modifiers.Any(m => m.Type == Parser.Tokenizer.Token.TokenType.Extern);

        if (isExtern)
            unit.DeclareFunction(symbol.FullyQualifiedName, funcType);
        else
            unit.DefineFunction(symbol.FullyQualifiedName, funcType);
    }

    public void GenerateMethodBody(MethodDeclarationNode node)
    {
        MethodDefinitionSymbol? symbol = (MethodDefinitionSymbol?)context.AnalysisContext.GetSymbol(node).Symbol;

        if (symbol == null)
        {
            Console.WriteLine($"Could not find symbol for {node.MethodName}!");
            return;
        }

        if (node.Modifiers.Any(m => m.Type == Parser.Tokenizer.Token.TokenType.Extern))
            return;

        LIRFunction func = unit.GetFunction(symbol.FullyQualifiedName) ?? throw new Exception($"Could not find function {symbol.FullyQualifiedName}!");
        StatementGenerator statementGen = new StatementGenerator(context, unit, func);

        foreach (var content in node.Body.Contents)
            if (content is StatementNode statementNode)
                statementGen.EmitStatement(statementNode);
    }
    
    public static LIRType ConvertType(TypeSymbol type) => type.KnownType switch
    {
        TypeSymbol.DefaultType.Void => LIRType.Void,
        TypeSymbol.DefaultType.Bool => LIRType.Boolean,
        TypeSymbol.DefaultType.I32 => LIRType.Int32,
        TypeSymbol.DefaultType.IPtr => LIRType.IntPtr,
        TypeSymbol.DefaultType.Struct => new LIRStructType(type.FullyQualifiedName),
        _ => LIRType.Void,
    };

    private static LIRFunctionType BuildFunctionType(MethodDefinitionSymbol symbol)
    {
        var parameters = symbol.Parameters.Select(p => new LIRParameter(p.Name, ConvertType(p.Type))).ToArray();
        return new LIRFunctionType(ConvertType(symbol.ReturnType), parameters);
    }
}
