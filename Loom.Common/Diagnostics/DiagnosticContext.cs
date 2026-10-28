
namespace Loom.Common.Diagnostics;

public class DiagnosticContext
{
    /// <summary> All currently reported diagnostics. </summary>
    public IReadOnlyList<Diagnostic> Diagnostics => diagnostics;

    private List<Diagnostic> diagnostics = new List<Diagnostic>();

    /// <summary> Reports a diagnostic. </summary>
    public void Report(DiagnosticDescription diagnostic, params object[] args)
    {
        Report(diagnostic, null, args);
    }

    /// <summary> Reports a diagnostic. </summary>
    public void Report(DiagnosticDescription diagnostic, DiagnosticPosition? position, params object[] args)
    {
        var message = args.Length > 0 ? string.Format(diagnostic.Format, args) : diagnostic.Format;
        diagnostics.Add(new Diagnostic(diagnostic.ID, diagnostic.Level, message, position));
    }

    /// <summary> Discards every diagnostic reported after <paramref name="checkpoint"/>. </summary>
    public void Rewind(int checkpoint) => diagnostics.RemoveRange(checkpoint, diagnostics.Count - checkpoint);

    /// <summary> Clears all reported diagnostics. </summary>
    public void Clear() => diagnostics.Clear();
}
