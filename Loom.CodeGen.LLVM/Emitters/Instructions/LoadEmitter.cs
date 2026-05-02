using LLVMSharp.Interop;
using Loom.LIR;
using Loom.LIR.OpCodes;

namespace Loom.CodeGen.LLVM.Emitters.Instructions;

[InstructionEmitter]
internal class LoadEmitter : IInstructionEmitter
{
    /// <inheritdoc/>
    public LIROpCode TargetOpCode => LIROpCode.Load;

    /// <inheritdoc/>
    public void Emit(LIRInstruction instruction, LLVMTranslationContext context)
    {
        if (instruction.Result == null)
            throw new InvalidOperationException("Load must produce a result.");

        if (instruction.Operands.Count < 1)
            throw new InvalidOperationException("Load requires a pointer operand.");

        LLVMValueRef ptr = context.ResolveValue(instruction.Operands[0]);

        if (ptr.TypeOf.Kind != LLVMTypeKind.LLVMPointerTypeKind)
            throw new InvalidOperationException("Load source must be a pointer.");

        var pointeeLirType = ((LIRPointerType)instruction.Operands[0].Type).PointeeType;
        LLVMTypeRef llvmType = context.TypeMap[pointeeLirType];

        LLVMValueRef value = context.Builder.BuildLoad2(llvmType, ptr, "loadtmp");

        context.ValueMap[instruction.Result] = value;
    }
}
