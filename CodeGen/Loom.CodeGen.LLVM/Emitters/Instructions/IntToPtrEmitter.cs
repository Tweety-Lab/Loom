using LLVMSharp.Interop;
using Loom.LIR;
using Loom.LIR.OpCodes;

namespace Loom.CodeGen.LLVM.Emitters.Instructions;

[InstructionEmitter]
internal class IntToPtrEmitter : IInstructionEmitter
{
    /// <inheritdoc/>
    public LIROpCode TargetOpCode => LIROpCode.IntToPtr;

    /// <inheritdoc/>
    public void Emit(LIRInstruction instruction, LLVMTranslationContext context)
    {
        if (instruction.Operands.Count < 1 || instruction.Result == null)
            throw new InvalidOperationException("inttoptr requires an integer operand and a result value.");

        LLVMValueRef value = context.ResolveValue(instruction.Operands[0]);
        LLVMTypeRef targetType = context.ResolveType(instruction.Result.Type);

        context.ValueMap[instruction.Result] = context.Builder.BuildIntToPtr(value, targetType);
    }
}
