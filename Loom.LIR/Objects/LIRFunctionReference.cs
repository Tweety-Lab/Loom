namespace Loom.LIR.Objects;

/// <summary> A typed function operand resolved to a concrete body during monomorphization. </summary>
internal abstract class LIRFunctionReference(string name, LIRFunctionType type) : LIRValue
{
    /// <summary> The linkage name of the referenced function instance. </summary>
    public string Name { get; } = name;

    /// <inheritdoc/>
    public override LIRFunctionType Type { get; } = type;
}
