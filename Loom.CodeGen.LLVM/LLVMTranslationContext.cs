using LLVMSharp.Interop;

namespace Loom.CodeGen.LLVM;

/// <summary>
/// The current state of translation.
/// </summary>
internal class LLVMTranslationContext
{
    public LLVMContextRef Context { get; set; }
    public LLVMModuleRef Module {  get; set; }

    public LLVMBuilderRef Builder { get; set; }
}
