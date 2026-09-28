using LLVMSharp.Interop;
using Loom.LIR;
using Loom.LIR.OpCodes;

namespace Loom.CodeGen.LLVM.Emitters.Instructions;

[InstructionEmitter]
internal class GetElementEmitter : IInstructionEmitter
{
    /// <inheritdoc/>
    public LIROpCode TargetOpCode => LIROpCode.GetElement;

    /// <inheritdoc/>
    public void Emit(LIRInstruction instruction, LLVMTranslationContext context)
    {
        if (instruction.Result == null)
            throw new InvalidOperationException("GetElement must produce a result value.");

        if (instruction.Operands.Count < 2)
            throw new InvalidOperationException("GetElement requires an array pointer and an index.");

        var arrayPointer = context.ResolveValue(instruction.Operands[0]);
        var index = context.ResolveValue(instruction.Operands[1]);
        var arrayType = (LIRArrayType)((LIRPointerType)instruction.Operands[0].Type).PointeeType;
        var zero = LLVMValueRef.CreateConstInt(LLVMTypeRef.Int32, 0);

        LLVMValueRef[] indexes = [zero, index];
        var elementPointer = context.Builder.BuildGEP2(context.ResolveType(arrayType), arrayPointer, indexes, "elementtmp");
        context.ValueMap[instruction.Result] = elementPointer;
    }
}
