using LLVMSharp.Interop;
using Loom.LIR;
using Loom.LIR.Objects;
using Loom.LIR.OpCodes;

namespace Loom.CodeGen.LLVM.Emitters.Instructions;

[InstructionEmitter]
internal class GetFieldEmitter : IInstructionEmitter
{
    /// <inheritdoc/>
    public LIROpCode TargetOpCode => LIROpCode.GetField;

    /// <inheritdoc/>
    public void Emit(LIRInstruction instruction, LLVMTranslationContext context)
    {
        if (instruction.Result == null)
            throw new InvalidOperationException("GetField must produce a result value.");

        if (instruction.Operands.Count < 2)
            throw new InvalidOperationException("GetField requires an instance pointer and a field operand.");

        if (instruction.Operands[1] is not LIRField field)
            throw new InvalidOperationException("GetField operand[1] must be an LIRField.");

        LLVMValueRef instancePtr = context.ResolveValue(instruction.Operands[0]);

        LLVMTypeRef structType = context.ResolveType(((LIRPointerType)instruction.Operands[0].Type).PointeeType);

        LLVMValueRef fieldPtr = context.Builder.BuildStructGEP2(structType, instancePtr, (uint)field.Index, "fieldtmp");

        context.ValueMap[instruction.Result] = fieldPtr;
    }
}