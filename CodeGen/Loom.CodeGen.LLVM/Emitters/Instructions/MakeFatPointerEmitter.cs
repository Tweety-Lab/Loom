using LLVMSharp.Interop;
using Loom.LIR;
using Loom.LIR.OpCodes;

namespace Loom.CodeGen.LLVM.Emitters.Instructions;

[InstructionEmitter]
internal class MakeFatPointerEmitter : IInstructionEmitter
{
    /// <inheritdoc/>
    public LIROpCode TargetOpCode => LIROpCode.MakeFatPointer;

    /// <inheritdoc/>
    public void Emit(LIRInstruction instruction, LLVMTranslationContext context)
    {
        if (instruction.Result?.Type is not LIRFatPointerType || instruction.Operands.Count != 2)
            throw new InvalidOperationException("makefatptr requires two pointers and a fat pointer result.");

        var value = LLVMValueRef.CreateConstNull(context.ResolveType(instruction.Result.Type));
        value = context.Builder.BuildInsertValue(value, context.ResolveValue(instruction.Operands[0]), 0, "fatptr.object");
        value = context.Builder.BuildInsertValue(value, context.ResolveValue(instruction.Operands[1]), 1, "fatptr.table");
        context.ValueMap[instruction.Result] = value;
    }
}