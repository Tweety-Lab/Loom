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

    /// <summary> Generates Loom Intermediate Representation (LIR) from all Abstract Syntax Tree (AST) roots in the program. </summary>
    /// <param name="roots"> All roots of the AST, one per source file. </param>
    /// <returns> The generated LIR unit. </returns>
    public LIRCompilationUnit Generate(IEnumerable<ProgramNode> roots)
    {
        // Eventually, we want to link units instead of compiling into just one

        var rootList = roots.ToList();
        unit = new LIRCompilationUnit(string.Join("+", rootList.Select(r => r.Name)));

        var contents = rootList.SelectMany(root => root.Modules).SelectMany(module => module.Body.Contents).ToList();

        foreach (var content in contents)
        {
            if (content is StructDeclarationNode structDeclaration)
                DeclareStruct(structDeclaration);
            else if (content is MethodDeclarationNode method)
                GenerateMethodDeclaration(method);
        }

        foreach (var content in contents)
        {
            if (content is StructDeclarationNode structDeclaration)
                GenerateStructMethodBodies(structDeclaration);
            else if (content is MethodDeclarationNode method)
                GenerateMethodBody(method);
        }

        return unit;
    }

    public void DeclareStruct(StructDeclarationNode node)
    {
        TypeSymbol? symbol = (TypeSymbol?)context.AnalysisContext.GetSymbol(node).Symbol;

        if (symbol == null)
        {
            Console.WriteLine($"Could not find symbol for {node.StructName}!");
            return;
        }

        LIRStruct structObj = unit.DefineStruct(symbol.FullyQualifiedName);

        foreach (var content in node.Body.Contents)
        {
            if (content is MethodDeclarationNode method)
                DeclareStructMethod(structObj, method);

            if (content is FieldDeclarationNode field)
                GenerateStructField(structObj, field);
        }
    }

    public void DeclareStructMethod(LIRStruct structObj, MethodDeclarationNode node)
    {
        MethodSymbol? symbol = (MethodSymbol?)context.AnalysisContext.GetSymbol(node).Symbol;

        if (symbol == null)
        {
            Console.WriteLine($"Could not find symbol for {node.MethodName}!");
            return;
        }

        bool isStatic = node.HasModifier(Parser.Tokenizer.Token.TokenType.Static);

        var funcType = BuildFunctionType(symbol, isStatic ? null : new LIRPointerType(structObj.Type));

        bool isExtern = node.HasModifier(Parser.Tokenizer.Token.TokenType.Extern);

        if (isExtern)
            structObj.DeclareMethod(symbol.FullyQualifiedName, funcType);
        else
            structObj.DefineMethod(symbol.FullyQualifiedName, funcType);
    }

    public void GenerateStructMethodBodies(StructDeclarationNode node)
    {
        TypeSymbol? symbol = (TypeSymbol?)context.AnalysisContext.GetSymbol(node).Symbol;

        if (symbol == null)
        {
            Console.WriteLine($"Could not find symbol for {node.StructName}!");
            return;
        }

        LIRStruct? structObj = unit.GetStruct(symbol.FullyQualifiedName);
        if (structObj == null)
            return;

        foreach (var content in node.Body.Contents)
            if (content is MethodDeclarationNode method && !method.HasModifier(Parser.Tokenizer.Token.TokenType.Extern))
                GenerateStructMethodBody(structObj, method);
    }

    public void GenerateStructMethodBody(LIRStruct structObj, MethodDeclarationNode node)
    {
        MethodSymbol? symbol = (MethodSymbol?)context.AnalysisContext.GetSymbol(node).Symbol;

        if (symbol == null)
        {
            Console.WriteLine($"Could not find symbol for {node.MethodName}!");
            return;
        }

        LIRFunction func = structObj.Methods.FirstOrDefault(m => m.Name == symbol.FullyQualifiedName) ?? throw new Exception($"Could not find function {symbol.FullyQualifiedName}!");

        StatementGenerator statementGen = new StatementGenerator(context, unit, func);

        foreach (var content in node.Body.Contents)
            if (content is StatementNode statementNode)
                statementGen.EmitStatement(statementNode);
    }

    public void GenerateStructField(LIRStruct structObj, FieldDeclarationNode node)
    {
        FieldSymbol? symbol = (FieldSymbol?)context.AnalysisContext.GetSymbol(node).Symbol;

        if (symbol == null)
        {
            Console.WriteLine($"Could not find symbol for {node.Variable.Name}!");
            return;
        }

        structObj.DeclareField(symbol.Name, ConvertType(symbol.Type!));
    }

    public void GenerateMethodDeclaration(MethodDeclarationNode node)
    {
        MethodSymbol? symbol = (MethodSymbol?)context.AnalysisContext.GetSymbol(node).Symbol;

        if (symbol == null)
        {
            Console.WriteLine($"Could not find symbol for {node.MethodName}!");
            return;
        }

        var funcType = BuildFunctionType(symbol);

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

        if (node.HasModifier(Parser.Tokenizer.Token.TokenType.Extern))
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
        TypeSymbol.DefaultType.Char => LIRType.Char,
        TypeSymbol.DefaultType.IPtr => LIRType.IntPtr,
        TypeSymbol.DefaultType.Struct => new LIRStructType(type.FullyQualifiedName),
        _ => throw new Exception($"Unknown type {type.KnownType}")
    };

    private static LIRFunctionType BuildFunctionType(MethodSymbol symbol, LIRType? instancePointerType = null)
    {
        var parameters = new List<LIRParameter>();

        if (instancePointerType != null)
            parameters.Add(new LIRParameter("self", instancePointerType));

        parameters.AddRange(symbol.Parameters.Select(p => new LIRParameter(p.Name, ConvertType(p.Type!))));

        return new LIRFunctionType(ConvertType(symbol.ReturnType!), parameters.ToArray());
    }
}
