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
        LLVMValueRef function = Context.Module.AddFunction(target.Name, GetFunctionType(target.Type));
        Context.FunctionMap[target] = function;


        for (int i = 0; i < target.ParameterValues.Count; i++)
        {
            LLVMValueRef llvmParam = function.GetParam((uint)i);
            llvmParam.Name = target.Type.Parameters[i].Name;
            Context.ValueMap[target.ParameterValues[i]] = llvmParam;
        }
    }

    /// <summary> Converts a <see cref="LIRFunctionType"/> to a LLVM <see cref="LLVMTypeRef"/>. </summary>
    public LLVMTypeRef GetFunctionType(LIRFunctionType type) => LLVMTypeRef.CreateFunction(Context.ResolveType(type.ReturnType), type.Parameters.Select(x => Context.ResolveType(x.Type)).ToArray());
}
