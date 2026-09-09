using LLVMSharp.Interop;
using Loom.LIR.OpCodes;

namespace Loom.CodeGen.LLVM.Emitters.Instructions;

[InstructionEmitter]
internal class CmpEqEmitter : ComparisonEmitter
{
    /// <inheritdoc/>
    public override LIROpCode TargetOpCode => LIROpCode.CmpEq;

    /// <inheritdoc/>
    protected override LLVMIntPredicate Predicate => LLVMIntPredicate.LLVMIntEQ;
}