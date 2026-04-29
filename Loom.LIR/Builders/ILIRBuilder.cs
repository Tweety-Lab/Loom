
using Loom.LIR.Objects;

namespace Loom.LIR.Builders;

/// <summary>
/// Base interface for all LIR builders.
/// </summary>
public interface ILIRBuilder<T> where T : ILIRObject
{
    /// <summary> The <see cref="ILIRObject"/> MetaData. </summary>
    Dictionary<string, string> MetaData { get; }

    /// <summary> Builds into a LIR </summary>
    T Build();
}
