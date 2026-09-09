using LLVMSharp.Interop;
using Loom.LIR;
using Loom.LIR.OpCodes;

namespace Loom.CodeGen.LLVM.Emitters.Instructions;

[InstructionEmitter]
internal class MulEmitter : IInstructionEmitter
{
    /// <inheritdoc/>
    public LIROpCode TargetOpCode => LIROpCode.Mul;

    /// <inheritdoc/>
    public void Emit(LIRInstruction instruction, LLVMTranslationContext context)
    {
        if (instruction.Result == null)
            throw new InvalidOperationException("Mul must produce a result.");

        if (instruction.Operands.Count < 2)
            throw new InvalidOperationException("Mul requires two operands.");

        LLVMValueRef lhs = context.ResolveValue(instruction.Operands[0]);
        LLVMValueRef rhs = context.ResolveValue(instruction.Operands[1]);

        LLVMValueRef result = context.Builder.BuildMul(lhs, rhs, "multmp");

        context.ValueMap[instruction.Result] = result;
    }
}