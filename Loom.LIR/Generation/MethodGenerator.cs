using Loom.Analyzer;
using Loom.Analyzer.Symbols;
using Loom.Common;
using Loom.LIR.Objects;
using Loom.Parser.AST;
using Loom.Parser.AST.Rules.Default;

namespace Loom.LIR.Generation;

/// <summary>
/// Generates the Loom Intermediate Representation (LIR) of the methods declared by a program, whether they are declared
/// inside a type or outside of one.
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

    /// <summary> Declares the signature of the method declared by <paramref name="node"/>. </summary>
    /// <param name="node"> The node declaring the method. </param>
    /// <param name="owningType"> The type owning the method, or null when the method is declared outside of a type. </param>
    public void GenerateMethodDeclaration(MethodDeclarationNode node, LIRTypeDeclaration? owningType = null)
    {
        MethodSymbol? symbol = (MethodSymbol?)context.AnalysisContext.GetSymbol(node).Symbol;

        if (symbol == null)
        {
            Console.WriteLine($"Could not find symbol for {node.MethodName}!");
            return;
        }

        if (symbol.IsGeneric)
            return;

        bool isExtern = symbol.IsExtern;

        // An instance method receives a pointer to the instance it belongs to as its first parameter
        LIRType? selfType = owningType != null && !symbol.IsStatic ? new LIRPointerType(owningType.Type) : null;

        var funcType = ASTGenerator.BuildFunctionType(symbol, selfType);

        if (owningType == null)
            unit.DefineFunction(symbol.FullyQualifiedName, funcType, !isExtern);
        else if (isExtern)
            owningType.DeclareMethod(symbol.FullyQualifiedName, funcType);
        else
            owningType.DefineMethod(symbol.FullyQualifiedName, funcType);
    }

    /// <summary> Emits the body of the method declared by <paramref name="node"/>. </summary>
    /// <param name="node"> The node declaring the method. </param>
    /// <param name="owningType"> The type owning the method, or null when the method is declared outside of a type. </param>
    public void GenerateMethodBody(MethodDeclarationNode node, LIRTypeDeclaration? owningType = null)
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

        LIRFunction function = GetFunction(symbol.FullyQualifiedName, owningType);

        StatementGenerator statementGen = new StatementGenerator(context, unit, function);

        foreach (var content in node.Body?.Contents ?? Enumerable.Empty<ASTNode>())
            if (content is StatementNode statementNode)
                statementGen.EmitStatement(statementNode);

        if (function.Type.ReturnType == LIRType.Void && function.LIRGenerator!.WritingBlock.Terminator == null)
            function.LIRGenerator.EmitReturn();
    }

    public void GenerateConstructorDeclaration(LIRTypeDeclaration owningType)
    {
        var selfType = new LIRPointerType(owningType.Type);
        var functionType = new LIRFunctionType(LIRType.Void, [new LIRParameter("self", selfType)]);

        owningType.DefineMethod($"{owningType.Name}::.ctor", functionType);
    }

    public void GenerateConstructorDeclaration(ConstructorDeclarationNode node, LIRTypeDeclaration owningType)
    {
        MethodSymbol? symbol = (MethodSymbol?)context.AnalysisContext.GetSymbol(node).Symbol;

        if (symbol == null)
        {
            Console.WriteLine($"Could not find symbol for {node.Name}!");
            return;
        }

        if (symbol.IsGeneric)
            return;

        var selfType = new LIRPointerType(owningType.Type);

        var parameters = new List<LIRParameter>
        {
            new("self", selfType)
        };

        parameters.AddRange(symbol.Parameters.Select(parameter => new LIRParameter(parameter.Name, ASTGenerator.ConvertStorageType(parameter.Type!))));

        var functionType = new LIRFunctionType(LIRType.Void, [.. parameters]);

        if (symbol.IsExtern)
            owningType.DeclareMethod(symbol.FullyQualifiedName, functionType);
        else
            owningType.DefineMethod(symbol.FullyQualifiedName, functionType);
    }

    public void GenerateConstructorBody(ConstructorDeclarationNode node, LIRTypeDeclaration owningType)
    {
        MethodSymbol? symbol = (MethodSymbol?)context.AnalysisContext.GetSymbol(node).Symbol;

        if (symbol == null)
        {
            Console.WriteLine($"Could not find symbol for {node.Name}!");
            return;
        }

        if (symbol.IsGeneric || symbol.IsExtern)
            return;

        LIRFunction function = GetFunction(symbol.FullyQualifiedName, owningType);

        StatementGenerator statementGenerator = new StatementGenerator(context, unit, function);

        statementGenerator.EmitFieldInitializers(node);

        foreach (var content in node.Body?.Contents ?? Enumerable.Empty<ASTNode>())
            if (content is StatementNode statement)
                statementGenerator.EmitStatement(statement);

        EmitImplicitReturn(function);
    }

    public void GenerateDefaultConstructorBody(ASTNode typeNode, LIRTypeDeclaration owningType)
    {
        LIRFunction? constructor = owningType.Methods.FirstOrDefault(method => method.Name == $"{owningType.Name}::.ctor");
        if (constructor == null)
            return;

        StatementGenerator statementGenerator = new StatementGenerator(context, unit, constructor);

        statementGenerator.EmitFieldInitializers(typeNode);

        EmitImplicitReturn(constructor);
    }

    private LIRFunction GetFunction(string name, LIRTypeDeclaration? owningType)
    {
        LIRFunction? function = owningType != null ? owningType.Methods.FirstOrDefault(m => m.Name == name) : unit.GetFunction(name);
        return function ?? throw new Exception($"Could not find function {name}!");
    }

    private static void EmitImplicitReturn(LIRFunction function)
    {
        if (function.Type.ReturnType == LIRType.Void && function.LIRGenerator!.WritingBlock.Terminator == null)
            function.LIRGenerator.EmitReturn();
    }
}