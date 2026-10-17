using Loom.Analyzer.Symbols;
using Loom.LIR.Objects;
using Loom.Parser.AST.Rules.Default;

namespace Loom.LIR.Generation;

internal partial class MethodGenerator
{
    /// <summary> Declares an instance destructor, retaining generic parameters until its containing type is specialized. </summary>
    public void GenerateDestructorDeclaration(MethodSymbol symbol, LIRTypeDeclaration containingType)
    {
        var signature = new LIRFunctionType(LIRType.Void, [new LIRParameter("self", new LIRPointerType(containingType.Type))], [.. containingType.TypeParameters]);

        containingType.DefineMethod(LinkageNameResolver.LinkageName(symbol), signature);
    }

    /// <summary> Emits a declared destructor body in its containing type's generic environment. </summary>
    public void GenerateDestructorBody(MethodSymbol symbol, LIRTypeDeclaration containingType, TypeSubstitution? substitution = null, TypeSymbol? ownerType = null)
    {
        if (symbol.DeclaringNode is not DestructorDeclarationNode node)
            return;

        var function = GetFunction(LinkageNameResolver.LinkageName(symbol), containingType);
        StatementGenerator statements = new(context, unit, function, this, substitution, ownerType);

        foreach (var statement in node.Body.Contents.OfType<StatementNode>())
            statements.EmitStatement(statement);

        statements.EndScope();
        EmitImplicitReturn(function);
    }

    /// <summary> Destroys a value in place, or destroys and releases an owned heap instance. </summary>
    public void DestroyInstance(LIRFunction function, LIRValue storage, TypeSymbol type)
    {
        var generator = function.LIRGenerator!;
        var destructor = type.Members.OfType<MethodSymbol>().FirstOrDefault(method => method.Kind == MethodSymbol.MethodKind.Destructor);

        if (type.IsValueType)
        {
            if (destructor != null)
                generator.EmitCallInstanced(GetMethodReference(destructor), storage);

            return;
        }

        var instance = generator.EmitLoad(storage);

        if (destructor == null)
        {
            FreeInstance(function, instance);
            return;
        }

        var destroyBlock = generator.CreateBlock("destroy.instance");
        var continueBlock = generator.CreateBlock("destroy.continue");

        generator.EmitCondBr(generator.EmitCmpNe(instance, new LIRNullValue(instance.Type)), destroyBlock, continueBlock);
        generator.SwitchTo(destroyBlock);
        generator.EmitCallInstanced(GetMethodReference(destructor), instance);
        FreeInstance(function, instance);
        generator.EmitBr(continueBlock);
        generator.SwitchTo(continueBlock);
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

    /// <summary> Releases an instance pointer previously returned by <see cref="AllocateInstance"/>. </summary>
    public void FreeInstance(LIRFunction function, LIRValue instance) => function.LIRGenerator!.EmitCall(GetRuntimeFunction("Standard::Memory::Unsafe::Free.iptr"), function.LIRGenerator.EmitPtrToInt(instance));

    private LIRFunction GetRuntimeFunction(string linkageName) => unit.GetFunction(linkageName) ?? throw new Exception($"Could not find required runtime function '{linkageName}'; the standard library must be part of the compilation unit.");
}
