using LLVMSharp;
using LLVMSharp.Interop;
using Loom.CodeGen.LLVM.Emitters;
using Loom.LIR;
using Loom.LIR.Objects;
using Loom.LIR.Passes;

namespace Loom.CodeGen.LLVM;

/// <summary>
/// A <see cref="LIRLayeredPass"/> that translates a <see cref="LIRCompilationUnit"/> into an LLVM module.
/// </summary>
public class LLVMTranslatorPass : LIRLayeredPass
{
    /// <summary> The translated LLVM module, set after <see cref="Run"/> has completed. </summary>
    public LLVMModuleRef Result { get; private set; }

    private LLVMTranslationContext translationContext = null!;
    private FunctionEmitter functionEmitter = null!;
    private BlockEmitter blockEmitter = null!;
    private InstructionEmitter instructionEmitter = null!;

    /// <inheritdoc />
    public override void Run(LIRCompilationUnit unit)
    {
        LLVMContextRef llvmContext = LLVMContextRef.Create();

        translationContext = new LLVMTranslationContext()
        {
            TypeMap =
            {
                [LIRType.Void] = LLVMTypeRef.Void,
                [LIRType.Int32] = LLVMTypeRef.Int32,
                [LIRType.Boolean] = LLVMTypeRef.Int1,
                [LIRType.IntPtr] = LLVMTypeRef.CreateIntPtr(LLVMTargetDataRef.FromStringRepresentation($"p:{System.IntPtr.Size * 8}:{System.IntPtr.Size * 8}:{System.IntPtr.Size * 8}")),
            },

            Context = llvmContext,
            Module = llvmContext.CreateModuleWithName(unit.Name),
            Builder = llvmContext.CreateBuilder()
        };

        // Register struct types so alloca/load/store can resolve them
        foreach (var structObj in unit.Structs)
            translationContext.TypeMap[structObj.Type] = llvmContext.CreateNamedStruct(structObj.Name);

        functionEmitter = new FunctionEmitter(translationContext);
        blockEmitter = new BlockEmitter(translationContext);
        instructionEmitter = new InstructionEmitter(translationContext);

        base.Run(unit);

        Result = translationContext.Module;
    }

    /// <inheritdoc/>
    protected override void RunOnFunction(LIRFunction func) => functionEmitter.Emit(func);

    /// <inheritdoc/>
    protected override void RunOnBlock(LIRBasicBlock block) => blockEmitter.Emit(block);

    /// <inheritdoc/>
    protected override void RunOnInstruction(LIRBasicBlock block, LIRInstruction inst) => instructionEmitter.Emit(block, inst);
}