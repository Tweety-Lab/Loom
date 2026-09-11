using LLVMSharp.Interop;
using Loom.LIR;
using Loom.LIR.OpCodes;

namespace Loom.CodeGen.LLVM.Emitters.Instructions;

[InstructionEmitter]
internal class AllocaEmitter : IInstructionEmitter
{
    /// <inheritdoc/>
    public LIROpCode TargetOpCode => LIROpCode.Alloca;

    /// <inheritdoc/>
    public void Emit(LIRInstruction instruction, LLVMTranslationContext context)
    {
        if (instruction.Result == null)
            throw new InvalidOperationException("Alloca must produce a result value.");

        LIRPointerType pointer = instruction.Result.Type as LIRPointerType ?? throw new InvalidOperationException("Alloca must produce a pointer type.");

        LLVMTypeRef llvmType = context.ResolveType(pointer.PointeeType);
        LLVMValueRef alloca = context.Builder.BuildAlloca(llvmType);

        context.ValueMap[instruction.Result] = alloca;
    }
}
