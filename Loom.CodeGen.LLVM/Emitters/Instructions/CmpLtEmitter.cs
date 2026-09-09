using LLVMSharp.Interop;
using Loom.LIR.OpCodes;

namespace Loom.CodeGen.LLVM.Emitters.Instructions;

[InstructionEmitter]
internal class CmpLtEmitter : ComparisonEmitter
{
    /// <inheritdoc/>
    public override LIROpCode TargetOpCode => LIROpCode.CmpLt;

    /// <inheritdoc/>
    protected override LLVMIntPredicate Predicate => LLVMIntPredicate.LLVMIntSLT;
}