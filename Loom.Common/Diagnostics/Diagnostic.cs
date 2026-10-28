
namespace Loom.Common.Diagnostics;

/// <summary>
/// Description of a Loom diagnostic.
/// </summary>
public readonly record struct DiagnosticDescription(string ID, Diagnostic.DiagnosticLevel Level, string Format);

public readonly record struct DiagnosticPosition(int Line, int Column);

/// <summary>
/// Loom diagnostic.
/// </summary>
public readonly record struct Diagnostic(string ID, Diagnostic.DiagnosticLevel Level, string Message, DiagnosticPosition? Position = null)
{
    public enum DiagnosticLevel
    {
        Error,
        Warning,
        Info
    }
}
