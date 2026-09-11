using LLVMSharp.Interop;
using Loom.LIR;
using Loom.LIR.Objects;

namespace Loom.CodeGen.LLVM;

/// <summary>
/// The current state of translation.
/// </summary>
internal class LLVMTranslationContext
{
    public Dictionary<LIRType, LLVMTypeRef> TypeMap { get; set; } = new Dictionary<LIRType, LLVMTypeRef>();

    public LLVMContextRef Context { get; set; }
    public LLVMModuleRef Module {  get; set; }

    public LLVMBuilderRef Builder { get; set; }

    public Dictionary<LIRFunction, LLVMValueRef> FunctionMap { get; set; } = new Dictionary<LIRFunction, LLVMValueRef>();
    public Dictionary<LIRBasicBlock, LLVMBasicBlockRef> BlockMap { get; set; } = new(); // TODO: Is this needed?
    public Dictionary<LIRValue, LLVMValueRef> ValueMap { get; set; } = new();

    /// <summary> Resolves an <see cref="LIRType"/> to its LLVM type, creating pointer types on demand. </summary>
    public LLVMTypeRef ResolveType(LIRType type)
    {
        if (TypeMap.TryGetValue(type, out LLVMTypeRef result))
            return result;

        if (type is LIRPointerType { PointeeType: var pointee })
        {
            // Opaque ptr pointees are typed (validated) at call sites via named struct signatures.
            return LLVMTypeRef.CreatePointer(ResolveType(pointee), 0);
        }

        throw new InvalidOperationException($"Unhandled LIR type: {type.GetType().Name}");
    }

    public LLVMValueRef ResolveValue(LIRValue value)
    {
        if (ValueMap.TryGetValue(value, out LLVMValueRef result))
            return result;

        if (value is LIRConstantIntValue intConst)
        {
            var llvmVal = LLVMValueRef.CreateConstInt(TypeMap[intConst.Type], (ulong)intConst.Value);
            ValueMap[value] = llvmVal;
            return llvmVal;
        }

        if (value is LIRConstantBoolValue boolConst)
        {
            var llvmVal = LLVMValueRef.CreateConstInt(TypeMap[boolConst.Type], boolConst.Value ? 1UL : 0UL);
            ValueMap[value] = llvmVal;
            return llvmVal;
        }

        if (value is LIRFunction funcValue)
            if (FunctionMap.TryGetValue(funcValue, out var llvmFunc))
                return llvmFunc;

        throw new InvalidOperationException("Could not resolve value");
    }
}
