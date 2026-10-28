namespace Loom.LIR.Metadata;

public sealed class LIRMetadataReference(MetadataType category, string name, LIRPointerType type) : LIRValue
{
    public MetadataType Category { get; } = category;
    public string Name { get; } = name;
    public override LIRPointerType Type { get; } = type;
}