using LLVMSharp.Interop;
using Loom.LIR.OpCodes;

namespace Loom.CodeGen.LLVM.Emitters.Instructions;

[InstructionEmitter]
internal class CmpNeEmitter : ComparisonEmitter
{
    /// <inheritdoc/>
    public override LIROpCode TargetOpCode => LIROpCode.CmpNe;

    /// <inheritdoc/>
    protected override LLVMIntPredicate Predicate => LLVMIntPredicate.LLVMIntNE;
}