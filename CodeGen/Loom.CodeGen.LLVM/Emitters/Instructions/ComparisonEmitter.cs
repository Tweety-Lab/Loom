using LLVMSharp.Interop;
using Loom.LIR;
using Loom.LIR.OpCodes;

namespace Loom.CodeGen.LLVM.Emitters.Instructions;

/// <summary>
/// Base class for integer comparison emitters.
/// </summary>
internal abstract class ComparisonEmitter : IInstructionEmitter
{
    /// <summary> The LLVM integer predicate used by this comparison. </summary>
    protected abstract LLVMIntPredicate Predicate { get; }

    /// <inheritdoc/>
    public abstract LIROpCode TargetOpCode { get; }

    /// <inheritdoc/>
    public void Emit(LIRInstruction instruction, LLVMTranslationContext context)
    {
        if (instruction.Result == null)
            throw new InvalidOperationException($"{TargetOpCode.Name} must produce a result.");

        if (instruction.Operands.Count < 2)
            throw new InvalidOperationException($"{TargetOpCode.Name} requires two operands.");

        LLVMValueRef lhs = context.ResolveValue(instruction.Operands[0]);
        LLVMValueRef rhs = context.ResolveValue(instruction.Operands[1]);

        LLVMValueRef result = context.Builder.BuildICmp(Predicate, lhs, rhs, "cmptmp");

        context.ValueMap[instruction.Result] = result;
    }
}