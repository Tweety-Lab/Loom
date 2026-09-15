using LLVMSharp.Interop;
using Loom.LIR.OpCodes;

namespace Loom.CodeGen.LLVM.Emitters.Instructions;

[InstructionEmitter]
internal class CmpGeEmitter : ComparisonEmitter
{
    /// <inheritdoc/>
    public override LIROpCode TargetOpCode => LIROpCode.CmpGe;

    /// <inheritdoc/>
    protected override LLVMIntPredicate Predicate => LLVMIntPredicate.LLVMIntSGE;
}