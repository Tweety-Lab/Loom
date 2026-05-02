using Loom.LIR.Objects;

namespace Loom.CodeGen.LLVM.Emitters;

internal class FunctionEmitter : Emitter<LIRFunction>
{
    /// <inheritdoc />
    public FunctionEmitter(LLVMTranslationContext context) : base(context) { }

    /// <inheritdoc />
    public override void Emit(LIRFunction value)
    {
        Console.WriteLine($"Emitting Function: {value.Name}");
    }
}
