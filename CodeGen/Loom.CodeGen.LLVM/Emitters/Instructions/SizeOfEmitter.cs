using LLVMSharp.Interop;
using Loom.LIR;
using Loom.LIR.OpCodes;

namespace Loom.CodeGen.LLVM.Emitters.Instructions;

[InstructionEmitter]
internal class SizeOfEmitter : IInstructionEmitter
{
    public LIROpCode TargetOpCode => LIROpCode.SizeOf;

    public void Emit(LIRInstruction instruction, LLVMTranslationContext context)
    {
        if (instruction.Result == null)
            throw new InvalidOperationException("SizeOf must produce a result value.");

        if (instruction.Operands is not [LIRTypeValue type])
            throw new InvalidOperationException("SizeOf requires a single type operand.");

        LLVMTypeRef llvmType = context.ResolveType(type.Type);

        if (!llvmType.IsSized)
            throw new InvalidOperationException("Cannot get size of unsized type.");

        context.ValueMap[instruction.Result] = llvmType.SizeOf;
    }
}
