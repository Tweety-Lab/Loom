using LLVMSharp;
using LLVMSharp.Interop;
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


        Result = LLVMModuleRef.CreateWithName(unit.Name);
    }
}
