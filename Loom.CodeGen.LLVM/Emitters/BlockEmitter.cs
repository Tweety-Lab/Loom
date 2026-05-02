using LLVMSharp.Interop;
using Loom.LIR;

namespace Loom.CodeGen.LLVM.Emitters;

internal class BlockEmitter : Emitter<LIRBasicBlock>
{
    /// <inheritdoc/>
    public BlockEmitter(LLVMTranslationContext context) : base(context) { }

    /// <inheritdoc/>
    public override void Emit(LIRBasicBlock target)
    {
        LLVMBasicBlockRef block = Context.FunctionMap[target.Parent].AppendBasicBlock(target.Name);
        Context.BlockMap[target] = block;
    }
}

