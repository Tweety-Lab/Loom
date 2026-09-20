namespace Loom.LIR.Objects;

/// <summary>
/// A Declared object (i.e., class, struct).
/// </summary>
public sealed class LIRDeclaredObject : LIRValueObject
{
    /// <summary> The fully qualified name of the <see cref="LIRDeclaredObject"/>, i.e. "Consumer::TestStruct". </summary>
    public string Name { get; }

    /// <inheritdoc/>
    public override LIRDeclaredObjectType Type { get; }

    /// <summary> The methods declared by this <see cref="LIRDeclaredObject"/>. </summary>
    public List<LIRFunction> Methods { get; } = new List<LIRFunction>();

    /// <summary> The fields declared by this <see cref="LIRDeclaredObject"/>. </summary>
    public List<LIRField> Fields { get; } = new List<LIRField>();

    /// <summary> Initializes a new instance of the <see cref="LIRDeclaredObject"/> class. </summary>
    private LIRDeclaredObject(string name, bool isValue)
    {
        Name = name;
        Type = new LIRDeclaredObjectType(name, isValue);
    }

    /// <summary> Creates a new <see cref="LIRDeclaredObject"/> ready for member emission. </summary>
    public static LIRDeclaredObject Define(string name, bool isValue) => new(name, isValue);

    /// <summary> Adds a method declaration (no body) to this <see cref="LIRDeclaredObject"/>. </summary>
    public LIRFunction DeclareMethod(string name, LIRFunctionType type)
    {
        var function = LIRFunction.Declare(name, type);
        Methods.Add(function);
        return function;
    }

    /// <summary> Adds a method with an empty body ready for instruction emission to this <see cref="LIRDeclaredObject"/>. </summary>
    public LIRFunction DefineMethod(string name, LIRFunctionType type)
    {
        var function = LIRFunction.Define(name, type);
        Methods.Add(function);
        return function;
    }

    /// <summary> Adds a field declaration to this <see cref="LIRDeclaredObject"/>. </summary>
    public LIRField DeclareField(string name, LIRType type)
    {
        var field = LIRField.Declare(name, Fields.Count, type);
        Fields.Add(field);
        return field;
    }
}