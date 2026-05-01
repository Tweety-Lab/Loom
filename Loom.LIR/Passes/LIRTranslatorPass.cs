
using Loom.LIR.Objects;

namespace Loom.LIR.Passes;

/// <summary> A <see cref="ICompilationPass"/> that translates a <see cref="LIRCompilationUnit"/> to another type. </summary>
public abstract class LIRTranslatorPass<T> : ICompilationPass
{
    /// <summary> The result of the translation. </summary>
    public abstract T Result { get; set; }

    /// <inheritdoc/>
    public abstract void Run(LIRCompilationUnit unit);
}
