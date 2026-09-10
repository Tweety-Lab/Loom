namespace Loom.LIR.Objects;

public class LIRCompilationUnit : ILIRObject
{
    /// <inheritdoc/>
    public Dictionary<string, string> MetaData { get; init; } = new();

    /// <summary> All functions owned by this <see cref="LIRCompilationUnit"/>. </summary>
    public List<LIRFunction> Functions { get; } = new List<LIRFunction>();

    /// <summary> All structs declared by this <see cref="LIRCompilationUnit"/>. </summary>
    public List<LIRStruct> Structs { get; } = new List<LIRStruct>();

    /// <summary> The name of this <see cref="LIRCompilationUnit"/>. </summary>
    public string Name => MetaData["Name"];

    /// <summary> Initializes a new instance of the <see cref="LIRCompilationUnit"/> class. </summary>
    public LIRCompilationUnit(string name) => MetaData.Add("Name", name);

    /// <summary> Gets a function by name. </summary>
    public LIRFunction? GetFunction(string name) => Functions.FirstOrDefault(f => f.Name == name);

    /// <summary> Defines a new function inside this <see cref="LIRCompilationUnit"/>. </summary>
    public LIRFunction DefineFunction(string name, LIRFunctionType type)
    {
        var function = LIRFunction.Define(name, type);
        Functions.Add(function);
        return function;
    }

    /// <summary> Declares a new function (no body) inside this <see cref="LIRCompilationUnit"/>. </summary>
    public LIRFunction DeclareFunction(string name, LIRFunctionType type)
    {
        var function = LIRFunction.Declare(name, type);
        Functions.Add(function);
        return function;
    }

    /// <summary> Adds a struct to this <see cref="LIRCompilationUnit"/>. </summary>
    public LIRStruct DefineStruct(string name)
    {
        var structObj = LIRStruct.Define(name);
        Structs.Add(structObj);
        return structObj;
    }

    /// <summary> Gets a struct by name. </summary>
    public LIRStruct? GetStruct(string name) => Structs.FirstOrDefault(s => s.Name == name);
}
