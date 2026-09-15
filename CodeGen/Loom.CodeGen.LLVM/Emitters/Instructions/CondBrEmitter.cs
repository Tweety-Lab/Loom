using LLVMSharp.Interop;
using Loom.LIR;
using Loom.LIR.OpCodes;

namespace Loom.CodeGen.LLVM.Emitters.Instructions;

[InstructionEmitter]
internal class CondBrEmitter : IInstructionEmitter
{
    /// <inheritdoc/>
    public LIROpCode TargetOpCode => LIROpCode.CondBr;

    /// <inheritdoc/>
    public void Emit(LIRInstruction instruction, LLVMTranslationContext context)
    {
        if (instruction.Operands.Count < 3)
            throw new InvalidOperationException("CondBr requires [condition, trueBlock, falseBlock] operands.");

        LLVMValueRef condition = context.ResolveValue(instruction.Operands[0]);
        LIRBasicBlock trueBlock = ((LIRBlockValue)instruction.Operands[1]).Block;
        LIRBasicBlock falseBlock = ((LIRBlockValue)instruction.Operands[2]).Block;

        context.Builder.BuildCondBr(condition, context.BlockMap[trueBlock], context.BlockMap[falseBlock]);
    }
}
