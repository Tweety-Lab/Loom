using Loom.LIR;
using Loom.LIR.OpCodes;

namespace Loom.CodeGen.LLVM.Emitters.Instructions;

[InstructionEmitter]
internal class PointerCastEmitter : IInstructionEmitter
{
    /// <inheritdoc/>
    public LIROpCode TargetOpCode => LIROpCode.PointerCast;

    /// <inheritdoc/>
    public void Emit(LIRInstruction instruction, LLVMTranslationContext context)
    {
        if (instruction.Result?.Type is not LIRPointerType || instruction.Operands.Count != 1 || instruction.Operands[0].Type is not LIRPointerType)
            throw new InvalidOperationException("pointercast requires a pointer operand and pointer result.");

        context.ValueMap[instruction.Result] = context.Builder.BuildBitCast(context.ResolveValue(instruction.Operands[0]), context.ResolveType(instruction.Result.Type), "pointercast");
    }
}