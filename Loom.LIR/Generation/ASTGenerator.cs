using Loom.Analyzer;
using Loom.Analyzer.Symbols;
using Loom.Common;
using Loom.LIR.Objects;
using Loom.Parser.AST;
using Loom.Parser.AST.Rules;
using Loom.Parser.AST.Rules.Default;

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
                DeclareTypeDeclaration(typeDeclaration);

            else if (content is MethodDeclarationNode method)
                GenerateMethodDeclaration(method);
        }

        foreach (var content in contents)
        {
            if (content is ITypeDeclarationNode typeDeclaration)
                GenerateTypeDeclarationMethodBodies(typeDeclaration);

            else if (content is MethodDeclarationNode method)
                GenerateMethodBody(method);
        }

        return unit;
    }

    public void DeclareTypeDeclaration(ITypeDeclarationNode node)
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

        LIRTypeDeclaration classObj = unit.DefineTypeDeclaration(symbol.FullyQualifiedName, symbol.IsValueType);

        // Default constructor
        if (!node.Body.Contents.Any(c => c is ConstructorDeclarationNode))
            DeclareTypeDeclarationConstructor(classObj);

        foreach (var content in node.Body.Contents)
        {
            if (content is ConstructorDeclarationNode constructor)
                DeclareTypeDeclarationConstructor(classObj, constructor);

            if (content is MethodDeclarationNode method)
                DeclareTypeDeclarationMethod(classObj, method);

            if (content is FieldDeclarationNode field)
                GenerateTypeDeclarationField(classObj, field);
        }
    }

    public void DeclareTypeDeclarationMethod(LIRTypeDeclaration declaredObj, MethodDeclarationNode node)
    {
        MethodSymbol? symbol = (MethodSymbol?)context.AnalysisContext.GetSymbol(node).Symbol;

        if (symbol == null)
        {
            Console.WriteLine($"Could not find symbol for {node.MethodName}!");
            return;
        }

        bool isStatic = symbol.IsStatic;
        bool isExtern = symbol.IsExtern;

        var funcType = BuildFunctionType(symbol, isStatic ? null : new LIRPointerType(declaredObj.Type));

        if (isExtern)
            declaredObj.DeclareMethod(symbol.FullyQualifiedName, funcType);
        else
            declaredObj.DefineMethod(symbol.FullyQualifiedName, funcType);
    }

    /// <summary> Declares the <c>.ctor</c> of a type, or the implicit default one when <paramref name="node"/> is null. </summary>
    public void DeclareTypeDeclarationConstructor(LIRTypeDeclaration declaredObj, ConstructorDeclarationNode? node = null)
    {
        // A constructor initializes the instance it is given, so it always receives one and never returns a value
        var selfType = new LIRPointerType(declaredObj.Type);

        if (node == null)
        {
            var funcType = new LIRFunctionType(LIRType.Void, [new LIRParameter("self", selfType)]);
            declaredObj.DefineMethod($"{declaredObj.Name}::.ctor", funcType);
            return;
        }

        MethodSymbol? symbol = (MethodSymbol?)context.AnalysisContext.GetSymbol(node).Symbol;

        if (symbol == null)
        {
            Console.WriteLine($"Could not find symbol for {node.Name}!");
            return;
        }

        var parameters = new List<LIRParameter> { new("self", selfType) };
        parameters.AddRange(symbol.Parameters.Select(p => new LIRParameter(p.Name, ConvertStorageType(p.Type!))));

        declaredObj.DefineMethod(symbol.FullyQualifiedName, new LIRFunctionType(LIRType.Void, [.. parameters]));
    }

    public void GenerateTypeDeclarationMethodBodies(ITypeDeclarationNode node)
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

        LIRTypeDeclaration? declaredObj = unit.GetTypeDeclaration(symbol.FullyQualifiedName);
        if (declaredObj == null)
            return;

        foreach (var content in node.Body.Contents)
        {
            if (content is MethodDeclarationNode methodNode)
            {
                if (context.AnalysisContext.GetSymbol(methodNode).Symbol is MethodSymbol methodSymbol && !methodSymbol.IsExtern)
                    GenerateTypeDeclarationMethodBody(declaredObj, methodNode);
            }

            if (content is ConstructorDeclarationNode constructorNode)
            {
                if (context.AnalysisContext.GetSymbol(constructorNode).Symbol is MethodSymbol methodSymbol && !methodSymbol.IsExtern)
                    GenerateTypeDeclarationConstructorBody(declaredObj, constructorNode);
            }
        }

        // A type without a declared constructor gets an implicit one that only initializes its fields
        if (node.Body.Contents.Any(c => c is ConstructorDeclarationNode))
            return;

        var defaultConstructor = declaredObj.Methods.FirstOrDefault(m => m.Name == $"{symbol.FullyQualifiedName}::.ctor");
        if (defaultConstructor == null)
            return;

        var statementGen = new StatementGenerator(context, unit, defaultConstructor);
        statementGen.EmitFieldInitializers((ASTNode)node);

        if (defaultConstructor.LIRGenerator!.WritingBlock.Terminator == null)
            defaultConstructor.LIRGenerator.EmitReturn();
    }

    public void GenerateTypeDeclarationMethodBody(LIRTypeDeclaration declaredObj, MethodDeclarationNode node)
    {
        MethodSymbol? symbol = (MethodSymbol?)context.AnalysisContext.GetSymbol(node).Symbol;

        if (symbol == null)
        {
            Console.WriteLine($"Could not find symbol for {node.MethodName}!");
            return;
        }

        LIRFunction func = declaredObj.Methods.FirstOrDefault(m => m.Name == symbol.FullyQualifiedName) ?? throw new Exception($"Could not find function {symbol.FullyQualifiedName}!");

        StatementGenerator statementGen = new StatementGenerator(context, unit, func);

        foreach (var content in node.Body?.Contents ?? Enumerable.Empty<ASTNode>())
            if (content is StatementNode statementNode)
                statementGen.EmitStatement(statementNode);

        if (func.Type.ReturnType == LIRType.Void && func.LIRGenerator!.WritingBlock.Terminator == null)
            func.LIRGenerator.EmitReturn();
    }

    public void GenerateTypeDeclarationConstructorBody(LIRTypeDeclaration declaredObj, ConstructorDeclarationNode node)
    {
        MethodSymbol? symbol = (MethodSymbol?)context.AnalysisContext.GetSymbol(node).Symbol;

        if (symbol == null)
        {
            Console.WriteLine($"Could not find symbol for {node.Name}!");
            return;
        }

        LIRFunction func = declaredObj.Methods.FirstOrDefault(m => m.Name == symbol.FullyQualifiedName) ?? throw new Exception($"Could not find function {symbol.FullyQualifiedName}!");

        StatementGenerator statementGen = new StatementGenerator(context, unit, func);

        statementGen.EmitFieldInitializers(node);

        foreach (var content in node.Body?.Contents ?? Enumerable.Empty<ASTNode>())
            if (content is StatementNode statementNode)
                statementGen.EmitStatement(statementNode);

        if (func.Type.ReturnType == LIRType.Void && func.LIRGenerator!.WritingBlock.Terminator == null)
            func.LIRGenerator.EmitReturn();
    }

    public void GenerateTypeDeclarationField(LIRTypeDeclaration declaredObj, FieldDeclarationNode node)
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

        foreach (var content in node.Body?.Contents ?? Enumerable.Empty<ASTNode>())
            if (content is StatementNode statementNode)
                statementGen.EmitStatement(statementNode);

        if (func.Type.ReturnType == LIRType.Void && func.LIRGenerator!.WritingBlock.Terminator == null)
            func.LIRGenerator.EmitReturn();
    }
    
    public static LIRType ConvertType(TypeSymbol type)
    {
        if (type is ArrayTypeSymbol array)
        {
            var arrayElementType = ConvertType(array.ElementType);
            return new LIRArrayType(array.ElementType.IsValueType ? arrayElementType : new LIRPointerType(arrayElementType), array.Size);
        }

        LIRType elementType = type.KnownType switch
        {
            TypeSymbol.DefaultType.Void => LIRType.Void,
            TypeSymbol.DefaultType.Bool => LIRType.Boolean,
            TypeSymbol.DefaultType.I32 => LIRType.Int32,
            TypeSymbol.DefaultType.Char => LIRType.Char,
            TypeSymbol.DefaultType.IPtr => LIRType.IntPtr,
            TypeSymbol.DefaultType.Struct => new LIRTypeDeclarationType(type.FullyQualifiedName, true),
            TypeSymbol.DefaultType.Class => new LIRTypeDeclarationType(type.FullyQualifiedName, false),
            _ => throw new Exception($"Unknown type {type.KnownType}")
        };

        return elementType;
    }

    private static LIRFunctionType BuildFunctionType(MethodSymbol symbol, LIRType? instancePointerType = null)
    {
        var parameters = new List<LIRParameter>();

        if (instancePointerType != null)
            parameters.Add(new LIRParameter("self", instancePointerType));

        parameters.AddRange(symbol.Parameters.Select(p => new LIRParameter(p.Name, ConvertStorageType(p.Type!))));

        return new LIRFunctionType(ConvertStorageType(symbol.ReturnType!), parameters.ToArray());
    }

    /// <summary> Converts <paramref name="type"/> to the LIR type it is stored as. </summary>
    public static LIRType ConvertStorageType(TypeSymbol type) => type is ArrayTypeSymbol || type.IsValueType || type.KnownType == TypeSymbol.DefaultType.Void ? ConvertType(type) : new LIRPointerType(ConvertType(type));
}
