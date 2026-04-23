
using Loom.Common.Exceptions;

namespace Loom.Common;

/// <summary>
/// Holds all information about a Loom compilation.
/// </summary>
public class CompilationContext
{
    /// <summary> All <see cref="LoomException"/>s that have occured during the compilation. </summary>
    public IReadOnlyList<LoomException> Exceptions => exceptions;

    /// <summary> Properties set by extensinos of <see cref="CompilationContext"/>. </summary>
    public Dictionary<string, object> ExtendedProperties { get; set; } = new();

    private List<LoomException> exceptions = new();

    /// <summary> Throws the given <see cref="LoomException"/>. </summary>
    public void ThrowException(LoomException exception) => exceptions.Add(exception);
}
