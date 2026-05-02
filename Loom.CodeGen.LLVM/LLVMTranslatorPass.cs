using LLVMSharp;
using LLVMSharp.Interop;
using Loom.CodeGen.LLVM.Emitters;
using Loom.LIR;
using Loom.LIR.Objects;
using Loom.LIR.Passes;

namespace Loom.CodeGen.LLVM;

public class LLVMTranslatorPass : LIRTranslatorPass<LLVMModuleRef>
{
    /// <inheritdoc/>
    public override LLVMModuleRef Result { get; set; }

    /// <inheritdoc />
    public override void Run(LIRCompilationUnit unit)
    {
        TypeMap.Add(LIRType.Void, LLVMTypeRef.Void);

        TypeMap.Add(LIRType.Int32, LLVMTypeRef.Int32);
        TypeMap.Add(LIRType.Boolean, LLVMTypeRef.Int1);

        LLVMContextRef llvmContext = LLVMContextRef.Create();

        LLVMTranslationContext translationContext = new LLVMTranslationContext()
        {
            Context = llvmContext,
            Module = llvmContext.CreateModuleWithName(unit.Name),
            Builder = llvmContext.CreateBuilder()
        };

        RunEmitters(translationContext, unit);

        Result = translationContext.Module;
    }

    private void RunEmitters(LLVMTranslationContext translationContext, LIRCompilationUnit unit)
    {
        // We navigate manually like this instead of using recursion because recursion would result in cases such as calling a function from a block thats emitted before the function

        FunctionEmitter functionEmitter = new FunctionEmitter(translationContext);

        foreach (var function in unit.Functions)
            functionEmitter.Emit(function);

        BlockEmitter blockEmitter = new BlockEmitter(translationContext);

        foreach (var function in unit.Functions)
            foreach (var block in function.Blocks)
                blockEmitter.Emit(block);
    }
}
