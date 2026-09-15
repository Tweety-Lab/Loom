using Loom.LIR;
using Loom.LIR.OpCodes;

namespace Loom.CodeGen.LLVM.Emitters.Instructions;

internal interface IInstructionEmitter
{
    LIROpCode TargetOpCode { get; }
    void Emit(LIRInstruction instruction, LLVMTranslationContext context);
}
