namespace Loom.LIR.Objects;

public class LIRCompilationUnit : ILIRObject
{
    /// <inheritdoc/>
    public Dictionary<string, string> MetaData { get; init; } = new();

    /// <summary> All functions owned by this <see cref="LIRCompilationUnit"/>, including methods declared in types. </summary>
    public IEnumerable<LIRFunction> AllFunctions => Functions.Concat(DeclaredObjects.SelectMany(o => o.Methods));

    /// <summary> All functions owned by this <see cref="LIRCompilationUnit"/>. </summary>
    public List<LIRFunction> Functions { get; } = new List<LIRFunction>();

    /// <summary> All <see cref="LIRDeclaredObject"/>s declared by this <see cref="LIRCompilationUnit"/>. </summary>
    public List<LIRDeclaredObject> DeclaredObjects { get; } = new List<LIRDeclaredObject>();

    /// <summary> The name of this <see cref="LIRCompilationUnit"/>. </summary>
    public string Name => MetaData["Name"];

    /// <summary> Initializes a new instance of the <see cref="LIRCompilationUnit"/> class. </summary>
    public LIRCompilationUnit(string name) => MetaData.Add("Name", name);

    /// <summary> Defines a new function inside this <see cref="LIRCompilationUnit"/>. </summary>
    public LIRFunction DefineFunction(string name, LIRFunctionType type, bool hasBody = true)
    {
        LIRFunction function;
        if (hasBody)
            function = LIRFunction.Define(name, type);
        else
            function = LIRFunction.Declare(name, type);

        Functions.Add(function);
        return function;
    }

    /// <summary> Adds a <see cref="LIRDeclaredObject"/> to this <see cref="LIRCompilationUnit"/>. </summary>
    public LIRDeclaredObject DefineDeclaredObject(string name, bool isValueType)
    {
        var declaredObj = LIRDeclaredObject.Define(name, isValueType);
        DeclaredObjects.Add(declaredObj);
        return declaredObj;
    }

    /// <summary> Gets a <see cref="LIRDeclaredObject"/> by name. </summary>
    public LIRDeclaredObject? GetDeclaredObject(string name) => DeclaredObjects.FirstOrDefault(s => s.Name == name);

    /// <summary> Gets a function by name. </summary>
    public LIRFunction? GetFunction(string name) => Functions.FirstOrDefault(f => f.Name == name);
}
