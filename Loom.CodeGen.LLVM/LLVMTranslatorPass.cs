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

        FunctionEmitter functionEmitter = new FunctionEmitter(translationContext);

        foreach (var function in unit.Functions)
            functionEmitter.Emit(function);

        Result = translationContext.Module;
    }
}
