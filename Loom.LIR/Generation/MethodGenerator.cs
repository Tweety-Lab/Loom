using Loom.Analyzer;
using Loom.Analyzer.Symbols;
using Loom.Common;
using Loom.LIR.Intrinsics;
using Loom.LIR.Objects;
using Loom.Parser.AST;
using Loom.Parser.AST.Rules.Default;

namespace Loom.LIR.Generation;

/// <summary>
/// Generates the Loom Intermediate Representation (LIR) of the methods declared by a program, whether they are declared
/// inside a type or outside of one.
/// </summary>
internal partial class MethodGenerator
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

        // A method of a generic type carries the type's parameters so that it is only emitted for its instantiations
        var typeParameters = GetOwningTypeParameters(node) is { } owning ? (IReadOnlyList<TypeParameterSymbol>)[.. owning, .. symbol.TypeParameters] : null;

        var funcType = ASTGenerator.BuildFunctionType(symbol, selfType, null, typeParameters);

        if (owningType == null)
            unit.DefineFunction(LinkageNameResolver.LinkageName(symbol), funcType, !isExtern);
        else if (isExtern)
            owningType.DeclareMethod(LinkageNameResolver.LinkageName(symbol), funcType);
        else
            owningType.DefineMethod(LinkageNameResolver.LinkageName(symbol), funcType);
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

        LIRFunction function = GetFunction(LinkageNameResolver.LinkageName(symbol), owningType);

        if (IIntrinsicEmitter.TryEmitIntrinsic(symbol, function))
            return;

        if (symbol.IsExtern)
            return;

        GenerateBody(node, function, null);
    }

    /// <summary> Declares the signature of a member of an instantiated generic type. </summary>
    public void GenerateMethodDeclaration(MethodSymbol symbol, LIRTypeDeclaration owningType, TypeSubstitution substitution)
    {
        LIRType? selfType = !symbol.IsStatic ? new LIRPointerType(owningType.Type) : null;
        var funcType = ASTGenerator.BuildFunctionType(symbol, selfType, substitution, symbol.TypeParameters);

        if (symbol.IsExtern)
            owningType.DeclareMethod(LinkageNameResolver.LinkageName(symbol), funcType);
        else
            owningType.DefineMethod(LinkageNameResolver.LinkageName(symbol), funcType);
    }

    /// <summary> Emits the body of a member of an instantiated generic type. </summary>
    public void GenerateMethodBody(MethodSymbol symbol, LIRTypeDeclaration owningType, TypeSubstitution substitution, TypeSymbol ownerType)
    {
        if (symbol.DeclaringNode is not MethodDeclarationNode node)
            return;

        LIRFunction function = GetFunction(LinkageNameResolver.LinkageName(symbol), owningType);

        if (IIntrinsicEmitter.TryEmitIntrinsic(symbol, function))
            return;

        if (symbol.IsExtern)
            return;

        GenerateBody(node, function, substitution, ownerType);
    }

    private void GenerateBody(MethodDeclarationNode node, LIRFunction function, TypeSubstitution? substitution, TypeSymbol? ownerType = null)
    {
        StatementGenerator statementGen = new(context, unit, function, this, substitution, ownerType);

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

    /// <summary> Gets a method's substituted function operand without materializing its body. </summary>
    public LIRValue GetMethodReference(MethodSymbol method)
    {
        string name = LinkageNameResolver.LinkageName(method);

        if (method.IsDefinition)
            return unit.GetFunction(name) ?? throw new InvalidOperationException($"Could not find function: {name}");

        LIRType? selfType = method is { IsStatic: false, ContainingType: { } type } ? new LIRPointerType(ASTGenerator.ConvertType(type)) : null;

        return new LIRMethodReference(name, method, ASTGenerator.BuildFunctionType(method, selfType, typeParameters: []));
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
