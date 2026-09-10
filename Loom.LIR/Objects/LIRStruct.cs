namespace Loom.LIR.Objects;

public sealed class LIRStruct : LIRValueObject
{
    /// <summary> The fully qualified name of the struct, i.e. "Consumer::TestStruct". </summary>
    public string Name { get; }

    /// <inheritdoc/>
    public override LIRStructType Type { get; }

    /// <summary> The methods declared by this struct. </summary>
    public List<LIRFunction> Methods { get; } = new List<LIRFunction>();

    /// <summary> Initializes a new instance of the <see cref="LIRStruct"/> class. </summary>
    private LIRStruct(string name)
    {
        Name = name;
        Type = new LIRStructType(name);
    }

    /// <summary> Creates a new <see cref="LIRStruct"/> ready for member emission. </summary>
    public static LIRStruct Define(string name) => new(name);

    /// <summary> Adds a method declaration (no body) to this struct. </summary>
    public LIRFunction DeclareMethod(string name, LIRFunctionType type)
    {
        var function = LIRFunction.Declare(name, type);
        Methods.Add(function);
        return function;
    }

    /// <summary> Adds a method with an empty body ready for instruction emission to this struct. </summary>
    public LIRFunction DefineMethod(string name, LIRFunctionType type)
    {
        var function = LIRFunction.Define(name, type);
        Methods.Add(function);
        return function;
    }
}