
namespace Loom.Common.Diagnostics;

public class DiagnosticContext
{
    /// <summary> All currently reported diagnostics. </summary>
    public IReadOnlyList<Diagnostic> Diagnostics => diagnostics;

    private List<Diagnostic> diagnostics = new List<Diagnostic>();

    /// <summary> Reports a diagnostic. </summary>
    public void Report(Diagnostic diagnostic) => diagnostics.Add(diagnostic);

    /// <summary> Clears all reported diagnostics. </summary>
    public void Clear() => diagnostics.Clear();
}
