
namespace Loom.LIR.Objects;

/// <summary>
/// A <see cref="ILIRObject"/> that can be passed as a <see cref="LIRValue"/>.
/// </summary>
public abstract class LIRValueObject : LIRValue, ILIRObject
{
    /// <inheritdoc/>
    public Dictionary<string, string> MetaData { get; init; } = new();
}
