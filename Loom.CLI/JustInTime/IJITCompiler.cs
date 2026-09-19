
using Loom.Analyzer.Symbols;

namespace Loom.CLI.JustInTime;

/// <summary>
/// Base interface for all Loom JIT backends.
/// </summary>
public interface IJITCompiler
{
    /// <summary> Initializes this JIT compiler for the given <see cref="LoomProject"/>. </summary>
    /// <returns> <see langword="true"/> if the JIT compiler was successfully initialized; otherwise, <see langword="false"/>. </returns>
    bool TryInitialize(LoomProject project);

    /// <summary> Executes the given method. </summary>
    /// <returns> <see langword="true"/> if the method was successfully executed; otherwise, <see langword="false"/>. </returns>
    bool TryExecute(MethodSymbol method);
}
