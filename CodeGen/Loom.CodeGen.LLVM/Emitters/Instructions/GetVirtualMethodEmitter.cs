using Loom.LIR;
using Loom.LIR.OpCodes;

namespace Loom.CodeGen.LLVM.Emitters.Instructions;

[InstructionEmitter]
internal class GetVirtualMethodEmitter : IInstructionEmitter
{
    /// <inheritdoc/>
    public LIROpCode TargetOpCode => LIROpCode.GetVirtualMethod;

    /// <inheritdoc/>
    public void Emit(LIRInstruction instruction, LLVMTranslationContext context)
    {
        if (instruction.Result?.Type is not LIRPointerType { PointeeType: LIRFunctionType } || instruction.Operands.Count != 2)
            throw new InvalidOperationException("getvirtualmethod requires a table pointer, a slot index and a function pointer result.");

        var pointerType = context.ResolveType(instruction.Result.Type);
        var address = context.Builder.BuildGEP2(pointerType, context.ResolveValue(instruction.Operands[0]), new[] { context.ResolveValue(instruction.Operands[1]) }, "vtable.slot");
        context.ValueMap[instruction.Result] = context.Builder.BuildLoad2(pointerType, address, "vtable.method");
    }
}