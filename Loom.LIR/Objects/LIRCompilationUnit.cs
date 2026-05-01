namespace Loom.LIR.Objects;

public class LIRCompilationUnit : ILIRObject
{
    /// <summary> All functions owned by this <see cref="LIRCompilationUnit"/>. </summary>
    public List<LIRFunction> Functions { get; } = new List<LIRFunction>();

    /// <inheritdoc/>
    public Dictionary<string, string> MetaData { get; init; } = new();

    /// <summary> Initializes a new instance of the <see cref="LIRCompilationUnit"/> class. </summary>
    public LIRCompilationUnit(List<LIRFunction> functions) => Functions = functions;

    /// <summary> Gets a function by name. </summary>
    public LIRFunction? GetFunction(string name) => Functions.FirstOrDefault(f => f.Name == name);

    /// <summary> Defines a new function inside this <see cref="LIRCompilationUnit"/>. </summary>
    public LIRFunction DefineFunction(string name, LIRFunctionType type)
    {
        var function = new LIRFunction(name, type);
        Functions.Add(function);
        return function;
    }
}
