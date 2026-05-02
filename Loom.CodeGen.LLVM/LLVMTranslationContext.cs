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

        throw new InvalidOperationException("Could not resolve value.");
    }
}
