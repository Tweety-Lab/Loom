using Loom.LIR;
using Loom.LIR.OpCodes;

namespace Loom.CodeGen.LLVM.Emitters.Instructions;

[InstructionEmitter]
internal class ExtractValueEmitter : IInstructionEmitter
{
    /// <inheritdoc/>
    public LIROpCode TargetOpCode => LIROpCode.ExtractValue;

    /// <inheritdoc/>
    public void Emit(LIRInstruction instruction, LLVMTranslationContext context)
    {
        if (instruction.Result == null || instruction.Operands.Count != 2 || instruction.Operands[1] is not LIRConstantIntValue index)
            throw new InvalidOperationException("extractvalue requires an aggregate and a constant index.");

        context.ValueMap[instruction.Result] = context.Builder.BuildExtractValue(context.ResolveValue(instruction.Operands[0]), checked((uint)index.Value), "extract");
    }
}