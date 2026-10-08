
namespace Loom.LIR.Objects;

public sealed class LIRGlobal : LIRValueObject
{
    /// <summary> The name of the global. </summary>
    public string Name { get; }

    /// <summary> The type of the value held by the global. </summary>
    public LIRType ValueType { get; }

    /// <summary> The value the global is initialized with. </summary>
    public LIRValue Initializer { get; }

    /// <summary> Whether the global holds constant memory that may not be written to. </summary>
    public bool IsConstant { get; }

    /// <inheritdoc />
    public override LIRType Type { get; }

    /// <summary> Initializes a new instance of the <see cref="LIRGlobal"/> class. </summary>
    private LIRGlobal(string name, LIRType valueType, LIRValue initializer, bool isConstant)
    {
        if (initializer.Type != valueType)
            throw new ArgumentException($"Global '{name}' of type '{valueType}' cannot be initialized by a value of type '{initializer.Type}'.");

        Name = name;
        ValueType = valueType;
        Initializer = initializer;
        IsConstant = isConstant;
        Type = new LIRPointerType(valueType);
    }

    /// <summary> Creates a new <see cref="LIRGlobal"/> holding <paramref name="initializer"/>. </summary>
    public static LIRGlobal Define(string name, LIRType valueType, LIRValue initializer, bool isConstant = true) => new(name, valueType, initializer, isConstant);
}
