using Loom.Analyzer;
using Loom.Analyzer.Symbols;
using Loom.Common;
using Loom.LIR.Objects;
using Loom.Parser.AST;
using Loom.Parser.AST.Rules.Default;

namespace Loom.LIR.Generation;

/// <summary>
/// Generates the Loom Intermediate Representation (LIR) of the types declared by a program.
/// </summary>
internal class TypeDeclarationGenerator
{
    private CompilationContext context;
    private LIRCompilationUnit unit;
    private MethodGenerator methodGenerator;

    /// <summary> Initializes a new instance of the <see cref="TypeDeclarationGenerator"/> class. </summary>
    public TypeDeclarationGenerator(CompilationContext context, LIRCompilationUnit unit)
    {
        this.context = context;
        this.unit = unit;
        methodGenerator = new MethodGenerator(context, unit);
    }

    public void DeclareTypeDeclaration(ITypeDeclarationNode node)
    {
        TypeSymbol? symbol = GetTypeSymbol(node);

        if (symbol == null)
            return;

        LIRTypeDeclaration classObj = unit.DefineTypeDeclaration(symbol.FullyQualifiedName, symbol.IsValueType);

        // Default constructor
        if (!node.Body.Contents.Any(c => c is ConstructorDeclarationNode))
            DeclareTypeDeclarationConstructor(classObj);

        foreach (var content in node.Body.Contents)
        {
            if (content is ConstructorDeclarationNode constructor)
                DeclareTypeDeclarationConstructor(classObj, constructor);

            if (content is MethodDeclarationNode method)
                methodGenerator.GenerateMethodDeclaration(method, classObj);

            if (content is FieldDeclarationNode field)
                GenerateTypeDeclarationField(classObj, field);
        }
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
        parameters.AddRange(symbol.Parameters.Select(p => new LIRParameter(p.Name, ASTGenerator.ConvertStorageType(p.Type!))));

        declaredObj.DefineMethod(symbol.FullyQualifiedName, new LIRFunctionType(LIRType.Void, [.. parameters]));
    }

    public void GenerateTypeDeclarationMethodBodies(ITypeDeclarationNode node)
    {
        TypeSymbol? symbol = GetTypeSymbol(node);

        if (symbol == null)
            return;

        LIRTypeDeclaration? declaredObj = unit.GetTypeDeclaration(symbol.FullyQualifiedName);
        if (declaredObj == null)
            return;

        foreach (var content in node.Body.Contents)
        {
            if (content is MethodDeclarationNode methodNode)
                methodGenerator.GenerateMethodBody(methodNode, declaredObj);

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

        declaredObj.DeclareField(symbol.Name, ASTGenerator.ConvertType(symbol.Type!));
    }

    /// <summary> Gets the symbol of the type declared by <paramref name="node"/>, or null when it has none. </summary>
    private TypeSymbol? GetTypeSymbol(ITypeDeclarationNode node)
    {
        TypeSymbol? symbol = null;

        if (node is StructDeclarationNode structNode)
            symbol = (TypeSymbol?)context.AnalysisContext.GetSymbol(structNode).Symbol;

        if (node is ClassDeclarationNode classNode)
            symbol = (TypeSymbol?)context.AnalysisContext.GetSymbol(classNode).Symbol;

        if (symbol == null)
            Console.WriteLine($"Could not find symbol for {node.Name}!");

        return symbol;
    }
}