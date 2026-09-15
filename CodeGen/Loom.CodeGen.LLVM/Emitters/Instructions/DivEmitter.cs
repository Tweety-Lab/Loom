using LLVMSharp.Interop;
using Loom.LIR;
using Loom.LIR.OpCodes;

namespace Loom.CodeGen.LLVM.Emitters.Instructions;

[InstructionEmitter]
internal class DivEmitter : IInstructionEmitter
{
    /// <inheritdoc/>
    public LIROpCode TargetOpCode => LIROpCode.Div;

    /// <inheritdoc/>
    public void Emit(LIRInstruction instruction, LLVMTranslationContext context)
    {
        if (instruction.Result == null)
            throw new InvalidOperationException("Div must produce a result.");

        if (instruction.Operands.Count < 2)
            throw new InvalidOperationException("Div requires two operands.");

        LLVMValueRef lhs = context.ResolveValue(instruction.Operands[0]);
        LLVMValueRef rhs = context.ResolveValue(instruction.Operands[1]);

        LLVMValueRef result = context.Builder.BuildSDiv(lhs, rhs, "divtmp");

        context.ValueMap[instruction.Result] = result;
    }
}