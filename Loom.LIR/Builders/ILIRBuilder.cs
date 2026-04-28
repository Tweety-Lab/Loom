
namespace Loom.LIR.Builders;

/// <summary>
/// Base interface for all LIR builders.
/// </summary>
public interface ILIRBuilder<T>
{
    /// <summary> Builds into a LIR </summary>
    T Build();
}
