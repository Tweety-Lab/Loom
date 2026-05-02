using LLVMSharp.Interop;
using Loom.LIR;
using Loom.LIR.OpCodes;

namespace Loom.CodeGen.LLVM.Emitters.Instructions;

[InstructionEmitter]
internal class ReturnEmitter : IInstructionEmitter
{
    /// <inheritdoc/>
    public LIROpCode TargetOpCode => LIROpCode.Return;

    /// <inheritdoc/>
    public void Emit(LIRInstruction instruction, LLVMTranslationContext context)
    {
        if (instruction.Operands.Count < 1)
        {
            context.Builder.BuildRetVoid();
            return;
        }

        LLVMValueRef value = context.ResolveValue(instruction.Operands[0]);
        context.Builder.BuildRet(value);
    }
}
