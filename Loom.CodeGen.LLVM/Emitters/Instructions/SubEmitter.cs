using LLVMSharp.Interop;
using Loom.LIR;
using Loom.LIR.OpCodes;

namespace Loom.CodeGen.LLVM.Emitters.Instructions;

[InstructionEmitter]
internal class SubEmitter : IInstructionEmitter
{
    /// <inheritdoc/>
    public LIROpCode TargetOpCode => LIROpCode.Sub;

    /// <inheritdoc/>
    public void Emit(LIRInstruction instruction, LLVMTranslationContext context)
    {
        if (instruction.Result == null)
            throw new InvalidOperationException("Sub must produce a result.");

        if (instruction.Operands.Count < 2)
            throw new InvalidOperationException("Sub requires two operands.");

        LLVMValueRef lhs = context.ResolveValue(instruction.Operands[0]);
        LLVMValueRef rhs = context.ResolveValue(instruction.Operands[1]);

        LLVMValueRef result = context.Builder.BuildSub(lhs, rhs, "subtmp");

        context.ValueMap[instruction.Result] = result;
    }
}