
namespace Loom.LIR.Objects;

/// <summary>
/// Base interface for all LIR objects (i.e., functions).
/// </summary>
public interface ILIRObject
{
    /// <summary> The metadata associated with the <see cref="ILIRObject"/>. </summary>
    Dictionary<string, string> MetaData { get; init; }
}
