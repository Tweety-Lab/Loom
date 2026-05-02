using LLVMSharp.Interop;
using Loom.LIR;
using Loom.LIR.Objects;

namespace Loom.CodeGen.LLVM.Emitters;

internal class FunctionEmitter : Emitter<LIRFunction>
{
    /// <inheritdoc />
    public FunctionEmitter(LLVMTranslationContext context) : base(context) { }

    /// <inheritdoc />
    public override void Emit(LIRFunction target)
    {
        Console.WriteLine($"Emitting Function: {target.Name}");

        Context.Module.AddFunction(target.Name, GetFunctionType(target.Type));
    }

    public LLVMTypeRef GetFunctionType(LIRFunctionType type)
    {
        return LLVMTypeRef.CreateFunction(TypeMap.Map[type.ReturnType], type.Parameters.Select(x => TypeMap.Map[x.Type]).ToArray());
    }
}
