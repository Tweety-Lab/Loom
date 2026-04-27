
namespace Loom.Common.Diagnostics;

/// <summary>
/// Loom diagnostic.
/// </summary>
public readonly record struct Diagnostic(Diagnostic.DiagnosticLevel Level, string Message)
{
    public enum DiagnosticLevel
    {
        Error,
        Warning,
        Info
    }
}
