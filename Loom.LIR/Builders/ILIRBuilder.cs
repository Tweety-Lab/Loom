
using Loom.LIR.Objects;

namespace Loom.LIR.Builders;

/// <summary>
/// Base interface for all LIR builders.
/// </summary>
public interface ILIRBuilder<T> where T : LIRObject
{
    /// <summary> The <see cref="LIRObject"/> MetaData. </summary>
    Dictionary<string, string> MetaData { get; }

    /// <summary> Builds into a LIR </summary>
    T Build();
}
