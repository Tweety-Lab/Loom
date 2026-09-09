using LLVMSharp.Interop;
using Loom.LIR.OpCodes;

namespace Loom.CodeGen.LLVM.Emitters.Instructions;

[InstructionEmitter]
internal class CmpGtEmitter : ComparisonEmitter
{
    /// <inheritdoc/>
    public override LIROpCode TargetOpCode => LIROpCode.CmpGt;

    /// <inheritdoc/>
    protected override LLVMIntPredicate Predicate => LLVMIntPredicate.LLVMIntSGT;
}