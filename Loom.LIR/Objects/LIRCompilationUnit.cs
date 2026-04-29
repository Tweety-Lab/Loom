namespace Loom.LIR.Objects;

public class LIRCompilationUnit : ILIRObject
{
    /// <summary> All functions owned by this <see cref="LIRCompilationUnit"/>. </summary>
    public List<LIRFunction> Functions { get; } = new List<LIRFunction>();

    /// <inheritdoc/>
    public Dictionary<string, string> MetaData { get; init; } = new();

    /// <summary> Initializes a new instance of the <see cref="LIRCompilationUnit"/> class. </summary>
    public LIRCompilationUnit(List<LIRFunction> functions) => Functions = functions;
}
