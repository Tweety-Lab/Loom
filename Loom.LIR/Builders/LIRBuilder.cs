
namespace Loom.LIR.Builders;

/// <summary>
/// The base class for all LIR builders.
/// </summary>
public abstract class LIRBuilder<T>
{
    /// <summary> Builds into a LIR </summary>
    public abstract T Build();
}
