

namespace Loom.Common.Exceptions;

/// <summary>
/// An <see cref="Exception"/> that occured during the <see cref="CompilationContext"/> process.
/// </summary>
public class LoomException : Exception
{
    /// <summary> Initializes a new instance of the <see cref="LoomException"/> class. </summary>
    public LoomException(string message) : base(message) { }
}
