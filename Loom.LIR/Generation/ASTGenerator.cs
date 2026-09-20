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
            if (content is ITypeDeclarationNode typeDeclaration)
                DeclareDeclaredType(typeDeclaration);

            else if (content is MethodDeclarationNode method)
                GenerateMethodDeclaration(method);
        }

        foreach (var content in contents)
        {
            if (content is ITypeDeclarationNode typeDeclaration)
                GenerateDeclaredTypeMethodBodies(typeDeclaration);

            else if (content is MethodDeclarationNode method)
                GenerateMethodBody(method);
        }

        return unit;
    }

    public void DeclareDeclaredType(ITypeDeclarationNode node)
    {
        TypeSymbol? symbol = null;

        if (node is StructDeclarationNode structNode)
            symbol = (TypeSymbol?)context.AnalysisContext.GetSymbol(structNode).Symbol;

        if (node is ClassDeclarationNode classNode)
            symbol = (TypeSymbol?)context.AnalysisContext.GetSymbol(classNode).Symbol;

        if (symbol == null)
        {
            Console.WriteLine($"Could not find symbol for {node.Name}!");
            return;
        }

        LIRDeclaredType classObj = unit.DefineDeclaredType(symbol.FullyQualifiedName, symbol.IsValueType);

        foreach (var content in node.Body.Contents)
        {
            if (content is MethodDeclarationNode method)
                DeclareDeclaredTypeMethod(classObj, method);

            if (content is FieldDeclarationNode field)
                GenerateDeclaredTypeField(classObj, field);
        }
    }

    public void DeclareDeclaredTypeMethod(LIRDeclaredType declaredObj, MethodDeclarationNode node)
    {
        MethodSymbol? symbol = (MethodSymbol?)context.AnalysisContext.GetSymbol(node).Symbol;

        if (symbol == null)
        {
            Console.WriteLine($"Could not find symbol for {node.MethodName}!");
            return;
        }

        bool isStatic = node.HasModifier(Parser.Tokenizer.Token.TokenType.Static);

        var funcType = BuildFunctionType(symbol, isStatic ? null : new LIRPointerType(declaredObj.Type));

        bool isExtern = node.HasModifier(Parser.Tokenizer.Token.TokenType.Extern);

        if (isExtern)
            declaredObj.DeclareMethod(symbol.FullyQualifiedName, funcType);
        else
            declaredObj.DefineMethod(symbol.FullyQualifiedName, funcType);
    }

    public void GenerateDeclaredTypeMethodBodies(ITypeDeclarationNode node)
    {
        TypeSymbol? symbol = null;

        if (node is StructDeclarationNode structNode)
            symbol = (TypeSymbol?)context.AnalysisContext.GetSymbol(structNode).Symbol;

        if (node is ClassDeclarationNode classNode)
            symbol = (TypeSymbol?)context.AnalysisContext.GetSymbol(classNode).Symbol;

        if (symbol == null)
        {
            Console.WriteLine($"Could not find symbol for {node.Name}!");
            return;
        }

        LIRDeclaredType? declaredObj = unit.GetDeclaredType(symbol.FullyQualifiedName);
        if (declaredObj == null)
            return;

        foreach (var content in node.Body.Contents)
            if (content is MethodDeclarationNode method && !method.HasModifier(Parser.Tokenizer.Token.TokenType.Extern))
                GenerateDeclaredTypeMethodBody(declaredObj, method);
    }

    public void GenerateDeclaredTypeMethodBody(LIRDeclaredType declaredObj, MethodDeclarationNode node)
    {
        MethodSymbol? symbol = (MethodSymbol?)context.AnalysisContext.GetSymbol(node).Symbol;

        if (symbol == null)
        {
            Console.WriteLine($"Could not find symbol for {node.MethodName}!");
            return;
        }

        LIRFunction func = declaredObj.Methods.FirstOrDefault(m => m.Name == symbol.FullyQualifiedName) ?? throw new Exception($"Could not find function {symbol.FullyQualifiedName}!");

        StatementGenerator statementGen = new StatementGenerator(context, unit, func);

        foreach (var content in node.Body.Contents)
            if (content is StatementNode statementNode)
                statementGen.EmitStatement(statementNode);
    }

    public void GenerateDeclaredTypeField(LIRDeclaredType declaredObj, FieldDeclarationNode node)
    {
        FieldSymbol? symbol = (FieldSymbol?)context.AnalysisContext.GetSymbol(node).Symbol;

        if (symbol == null)
        {
            Console.WriteLine($"Could not find symbol for {node.Variable.Name}!");
            return;
        }

        declaredObj.DeclareField(symbol.Name, ConvertType(symbol.Type!));
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
        TypeSymbol.DefaultType.Struct => new LIRDeclaredTypeType(type.FullyQualifiedName, true),
        TypeSymbol.DefaultType.Class => new LIRDeclaredTypeType(type.FullyQualifiedName, false),
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
