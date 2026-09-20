
namespace Loom.Common.Diagnostics;

public class DiagnosticContext
{
    /// <summary> All currently reported diagnostics. </summary>
    public IReadOnlyList<Diagnostic> Diagnostics => diagnostics;

    private List<Diagnostic> diagnostics = new List<Diagnostic>();

    /// <summary> Reports a diagnostic. </summary>
    public void Report(Diagnostic diagnostic, params object[] args)
    {
        if (args.Length > 0)
            diagnostics.Add(diagnostic with { Message = string.Format(diagnostic.Message, args) });
        else
            diagnostics.Add(diagnostic);
    }

    /// <summary> Reports a diagnostic. </summary>
    public void Report(Diagnostic diagnostic, DiagnosticPosition? position, params object[] args)
    {
        var d = diagnostic with { Position = position };

        if (args.Length > 0)
            diagnostics.Add(d with { Message = string.Format(d.Message, args) });
        else
            diagnostics.Add(d);
    }

    /// <summary> Clears all reported diagnostics. </summary>
    public void Clear() => diagnostics.Clear();
}
