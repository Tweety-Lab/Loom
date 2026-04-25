
using Loom.Common.Diagnostics;

namespace Loom.Common;

/// <summary>
/// Holds all information about a Loom compilation.
/// </summary>
public class CompilationContext
{
    /// <summary> Properties set by extensions of <see cref="CompilationContext"/>. </summary>
    public Dictionary<string, object> ExtendedProperties { get; set; } = new();

    /// <summary> The diagnostic results of the compilation. </summary>
    public DiagnosticContext DiagnosticContext { get; } = new();
}
