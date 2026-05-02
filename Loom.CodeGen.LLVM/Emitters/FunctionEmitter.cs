using LLVMSharp.Interop;
using Loom.LIR;
using Loom.LIR.Objects;

namespace Loom.CodeGen.LLVM.Emitters;

internal class FunctionEmitter : Emitter<LIRFunction>
{
    /// <inheritdoc/>
    public FunctionEmitter(LLVMTranslationContext context) : base(context) { }

    /// <inheritdoc/>
    public override void Emit(LIRFunction target)
    {
        Console.WriteLine($"Emitting Function: {target.Name}");

        LLVMValueRef function = Context.Module.AddFunction(target.Name, GetFunctionType(target.Type));
        Context.FunctionMap[target] = function;
    }

    /// <summary> Converts a <see cref="LIRFunctionType"/> to a LLVM <see cref="LLVMTypeRef"/>. </summary>
    public LLVMTypeRef GetFunctionType(LIRFunctionType type) => LLVMTypeRef.CreateFunction(Context.TypeMap[type.ReturnType], type.Parameters.Select(x => Context.TypeMap[x.Type]).ToArray());
}
