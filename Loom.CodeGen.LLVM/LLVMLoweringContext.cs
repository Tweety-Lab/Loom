using LLVMSharp.Interop;
using Loom.LIR;
using Loom.LIR.Objects;

namespace Loom.CodeGen.LLVM;

public class LLVMLoweringContext
{
    public LLVMContextRef Context { get; }
    public LLVMModuleRef Module { get; }

    public LLVMBuilderRef Builder { get; }

    public Dictionary<LIRValue, LLVMValueRef> Values { get; } = new();
    public Dictionary<LIRFunction, (LLVMValueRef Value, LLVMTypeRef Type)> Functions { get; } = new();

    public LLVMLoweringContext(string moduleName)
    {
        Context = LLVMContextRef.Create();
        Module = Context.CreateModuleWithName(moduleName);
        Builder = LLVMBuilderRef.Create(Context);
    }
}
