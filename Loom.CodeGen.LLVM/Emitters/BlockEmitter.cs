using Loom.LIR;

namespace Loom.CodeGen.LLVM.Emitters;

internal class BlockEmitter : Emitter<LIRBasicBlock>
{
    /// <inheritdoc />
    public BlockEmitter(LLVMTranslationContext context) : base(context) { }

    /// <inheritdoc />
    public override void Emit(LIRBasicBlock target)
    {
        Console.WriteLine($"Emitting Block: {target.Name}");
    }
}

