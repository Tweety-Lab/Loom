
using Loom.LIR.Objects;

namespace Loom.LIR.Builders;

/// <summary>
/// Base class for all LIR builders.
/// </summary>
public abstract class LIRBuilder<T> where T : ILIRObject
{
    public T BuildResult
    {
        get
        {
            if (field != null)
                return field;

            field = Rebuild();
            return field;
        }
    }

    /// <summary> The <see cref="ILIRObject"/> MetaData. </summary>
    public Dictionary<string, string> MetaData { get; } = new();

    /// <summary> Rebuilds the object into a LIR </summary>
    public abstract T Rebuild();
}
