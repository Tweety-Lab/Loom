using LLVMSharp.Interop;
using Loom.LIR;
using Loom.LIR.OpCodes;

namespace Loom.CodeGen.LLVM.Emitters.Instructions;

[InstructionEmitter]
internal class StoreEmitter : IInstructionEmitter
{
    /// <inheritdoc/>
    public LIROpCode TargetOpCode => LIROpCode.Store;

    /// <inheritdoc/>
    public void Emit(LIRInstruction instruction, LLVMTranslationContext context)
    {
        // TODO: Is doing these checks here dumb? Should we just leave it to the verifier passes?
        if (instruction.Operands.Count < 2)
            throw new InvalidOperationException("Store requires [value, pointer] operands.");

        LIRValue valueOperand = instruction.Operands[0];
        LLVMValueRef value = context.ResolveValue(valueOperand);

        LIRValue pointerOperand = instruction.Operands[1];
        LLVMValueRef ptr = context.ResolveValue(pointerOperand);

        LLVMTypeRef ptrType = ptr.TypeOf;
        if (ptrType.Kind != LLVMTypeKind.LLVMPointerTypeKind)
            throw new InvalidOperationException("Store target must be a pointer.");

        context.Builder.BuildStore(value, ptr);
    }
}
