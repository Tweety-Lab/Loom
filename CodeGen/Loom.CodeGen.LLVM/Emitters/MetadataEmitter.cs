using LLVMSharp.Interop;
using Loom.LIR.Metadata;
using Loom.LIR.Objects;

namespace Loom.CodeGen.LLVM.Emitters;

internal class MetadataEmitter : Emitter<(MetadataType, List<LIRMetadataValue>)>
{
    /// <inheritdoc/>
    public MetadataEmitter(LLVMTranslationContext context) : base(context) { }

    /// <inheritdoc/>
    public override void Emit((MetadataType, List<LIRMetadataValue>) info)
    {
        foreach (var value in info.Item2)
            EmitSingle(info.Item1, value);
    }

    public void EmitSingle(MetadataType type, LIRMetadataValue value)
    {
        LLVMTypeRef valueType = Context.ResolveType(value.ValueType);
        LLVMValueRef metadata = Context.Module.AddGlobal(valueType, $"{type.ToString()}{value.Name}");

        metadata.Initializer = Context.ResolveValue(value.Initializer);

        metadata.Linkage = value.IsConstant ? LLVMLinkage.LLVMPrivateLinkage : LLVMLinkage.LLVMInternalLinkage;
        metadata.IsGlobalConstant = value.IsConstant;
        metadata.HasUnnamedAddr = value.IsConstant;

        Context.ValueMap[value] = metadata;
    }
}
