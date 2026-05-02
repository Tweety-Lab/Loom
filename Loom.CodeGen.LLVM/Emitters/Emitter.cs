
namespace Loom.CodeGen.LLVM.Emitters;

/// <summary>
/// Emitter that emits values into a <see cref="LLVMTranslationContext"/>.
/// </summary>
internal abstract class Emitter<T>
{
    /// <summary> The translation context this emitter relies on. </summary>
    public LLVMTranslationContext Context { get; }

    /// <summary> Initializes a new instance of the <see cref="Emitter{T}"/> class. </summary>
    public Emitter(LLVMTranslationContext context) => Context = context;

    /// <summary> Emits a <typeparamref name="T"/> into the translation context. </summary>
    public abstract void Emit(T target);
}
