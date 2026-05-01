using Loom.LIR.Objects;

namespace Loom.LIR.Passes;

/// <summary>
/// Base interface for all Loom Intermediate Representation passes that run on a <see cref="LIRCompilationUnit"/>.
/// </summary>
public interface ICompilationPass
{
    /// <summary> Runs the <see cref="ICompilationPass"/> on the specified <see cref="LIRCompilationUnit"/>. </summary>
    void Run(LIRCompilationUnit unit);
}