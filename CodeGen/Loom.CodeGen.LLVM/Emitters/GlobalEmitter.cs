using LLVMSharp.Interop;
using Loom.LIR.Objects;

namespace Loom.CodeGen.LLVM.Emitters;

internal class GlobalEmitter : Emitter<LIRGlobal>
{
    /// <inheritdoc/>
    public GlobalEmitter(LLVMTranslationContext context) : base(context) { }

    /// <inheritdoc/>
    public override void Emit(LIRGlobal target)
    {
        LLVMTypeRef valueType = Context.ResolveType(target.ValueType);
        LLVMValueRef global = Context.Module.AddGlobal(valueType, target.Name);

        global.Initializer = Context.ResolveValue(target.Initializer);

        global.Linkage = target.IsConstant ? LLVMLinkage.LLVMPrivateLinkage : LLVMLinkage.LLVMInternalLinkage;
        global.IsGlobalConstant = target.IsConstant;
        global.HasUnnamedAddr = target.IsConstant;

        Context.ValueMap[target] = global;
    }
}
