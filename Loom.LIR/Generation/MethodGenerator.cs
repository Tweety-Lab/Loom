using Loom.Analyzer;
using Loom.Analyzer.Symbols;
using Loom.Common;
using Loom.LIR.Intrinsics;
using Loom.LIR.Objects;
using Loom.LIR.OpCodes;
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

        bool isExtern = symbol.IsExtern;

        // An instance method receives a pointer to the instance it belongs to as its first parameter
        LIRType? selfType = owningType != null && !symbol.IsStatic ? new LIRPointerType(owningType.Type) : null;

        var funcType = ASTGenerator.BuildFunctionType(symbol, selfType);

        if (owningType == null)
            unit.DefineFunction(symbol.LinkageName, funcType, !isExtern);
        else if (isExtern)
            owningType.DeclareMethod(symbol.LinkageName, funcType);
        else
            owningType.DefineMethod(symbol.LinkageName, funcType);
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

        LIRFunction function = GetFunction(symbol.LinkageName, owningType);

        if (IIntrinsicEmitter.TryEmitIntrinsic(symbol, function))
            return;

        if (symbol.IsExtern)
            return;

        GenerateBody(node, function, null);
    }

    /// <summary> Gets the instantiation of the generic method <paramref name="symbol"/> for <paramref name="typeArguments"/>, generating it on first use. </summary>
    /// <param name="symbol"> The generic method to instantiate. </param>
    /// <param name="typeArguments"> The concrete type arguments to bind the method's type parameters to. </param>
    /// <param name="callSite"> The node requesting the instantiation. </param>
    public LIRFunction GetOrCreateInstance(MethodSymbol symbol, IReadOnlyList<TypeSymbol> typeArguments, ASTNode callSite)
    {
        var substitution = TypeSubstitution.Zip(symbol, typeArguments);
        string instanceName = $"{symbol.LinkageName}<{string.Join(", ", typeArguments.Select(t => t.FullyQualifiedName))}>";

        LIRFunction? instance = unit.GetFunction(instanceName);
        if (instance != null)
            return instance;

        if (symbol.DeclaringNode is not MethodDeclarationNode node)
            throw new Exception($"Could not find the declaration of generic method {symbol.LinkageName}.");

        LIRTypeDeclaration? owningType = null;
        var typeNode = context.AnalysisContext.FirstAncestorOrSelf<ITypeDeclarationNode>(node);

        if (typeNode != null && context.AnalysisContext.GetSymbol((ASTNode)typeNode).Symbol is TypeSymbol owningSymbol)
            owningType = unit.GetTypeDeclaration(owningSymbol.FullyQualifiedName);

        LIRType? selfType = owningType != null && !symbol.IsStatic ? new LIRPointerType(owningType.Type) : null;
        var functionType = ASTGenerator.BuildFunctionType(symbol, selfType, substitution);

        // Register the instance before generating its body so recursive calls resolve to it
        instance = owningType == null ? unit.DefineFunction(instanceName, functionType, !symbol.IsExtern) : symbol.IsExtern ? owningType.DeclareMethod(instanceName, functionType) : owningType.DefineMethod(instanceName, functionType);

        if (IIntrinsicEmitter.TryEmitIntrinsic(symbol, instance))
            return instance;

        if (!symbol.IsExtern)
            GenerateBody(node, instance, substitution);

        return instance;
    }

    private void GenerateBody(MethodDeclarationNode node, LIRFunction function, TypeSubstitution? substitution)
    {
        StatementGenerator statementGen = new(context, unit, function, this, substitution);

        foreach (var content in node.Body?.Contents ?? Enumerable.Empty<ASTNode>())
            if (content is StatementNode statementNode)
                statementGen.EmitStatement(statementNode);

        statementGen.EndScope();

        MethodSymbol? symbol = (MethodSymbol?)context.AnalysisContext.GetSymbol(node).Symbol;

        // Intrinsic methods define their own return
        if (symbol != null && IIntrinsicEmitter.HasIntrinsicEmitter(symbol))
            return;

        EmitImplicitReturn(function);
    }

    public void GenerateConstructorDeclaration(LIRTypeDeclaration owningType)
    {
        var selfType = new LIRPointerType(owningType.Type);
        var functionType = new LIRFunctionType(LIRType.Void, [new LIRParameter("self", selfType)], []);

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

        List<LIRTypeParameter> typeParameters = symbol.TypeParameters.Select(tp => new LIRTypeParameter(tp.Name)).ToList();

        var functionType = new LIRFunctionType(LIRType.Void, [.. parameters], typeParameters.ToArray());

        if (symbol.IsExtern)
            owningType.DeclareMethod(symbol.LinkageName, functionType);
        else
            owningType.DefineMethod(symbol.LinkageName, functionType);
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

        LIRFunction function = GetFunction(symbol.LinkageName, owningType);

        StatementGenerator statementGenerator = new StatementGenerator(context, unit, function, this);

        statementGenerator.EmitFieldInitializers(node);

        foreach (var content in node.Body?.Contents ?? Enumerable.Empty<ASTNode>())
            if (content is StatementNode statement)
                statementGenerator.EmitStatement(statement);

        statementGenerator.EndScope();

        EmitImplicitReturn(function);
    }

    public void GenerateDefaultConstructorBody(ASTNode typeNode, LIRTypeDeclaration owningType)
    {
        LIRFunction? constructor = owningType.Methods.FirstOrDefault(method => method.Name == $"{owningType.Name}::.ctor");
        if (constructor == null)
            return;

        StatementGenerator statementGenerator = new StatementGenerator(context, unit, constructor, this);

        statementGenerator.EmitFieldInitializers(typeNode);
        statementGenerator.EndScope();

        EmitImplicitReturn(constructor);
    }

    /// <summary> Allocates storage for a new instance of <paramref name="instanceType"/> on the heap. </summary>
    /// <param name="function"> The function the allocation is emitted into. </param>
    /// <param name="instanceType"> The type of the instance being allocated. </param>
    /// <returns> The address of the allocated, not yet initialized instance. </returns>
    public LIRTempValue AllocateInstance(LIRFunction function, LIRPointerType instanceType)
    {
        LIRGenerator generator = function.LIRGenerator!;
        LIRFunction allocate = GetRuntimeFunction("Standard::Memory::Unsafe::Allocate.i64");

        return generator.EmitIntToPtr(generator.EmitCall(allocate, generator.EmitSizeOf(instanceType.PointeeType)), instanceType);
    }

    /// <summary> Releases storage previously returned by <see cref="AllocateInstance"/>. </summary>
    /// <param name="function"> The function the release is emitted into. </param>
    /// <param name="storage"> The address of the pointer holding the instance to release. </param>
    public void FreeInstance(LIRFunction function, LIRValue storage)
    {
        LIRGenerator generator = function.LIRGenerator!;
        LIRFunction free = GetRuntimeFunction("Standard::Memory::Unsafe::Free.iptr");

        LIRValue pointer = generator.EmitBeforeTerminator(LIROpCode.Load, ((LIRPointerType)storage.Type).PointeeType, storage);
        LIRValue address = generator.EmitBeforeTerminator(LIROpCode.PtrToInt, LIRType.IntPtr, pointer);

        generator.EmitBeforeTerminator(LIROpCode.Call, free.Type.ReturnType, free, address);
    }

    private LIRFunction GetFunction(string name, LIRTypeDeclaration? owningType)
    {
        LIRFunction? function = owningType != null ? owningType.Methods.FirstOrDefault(m => m.Name == name) : unit.GetFunction(name);
        return function ?? throw new Exception($"Could not find function {name}!");
    }

    private LIRFunction GetRuntimeFunction(string linkageName) => unit.GetFunction(linkageName) ?? throw new Exception($"Could not find required runtime function '{linkageName}'; the standard library must be part of the compilation unit.");

    private static void EmitImplicitReturn(LIRFunction function)
    {
        if (function.Type.ReturnType == LIRType.Void && function.LIRGenerator!.WritingBlock.Terminator == null)
            function.LIRGenerator.EmitReturn();
    }
}