
namespace Loom.LIR.Objects;

public sealed class LIRMetadataValue : LIRValueObject
{
    /// <summary> The name of the <see cref="LIRMetadataValue"/>. </summary>
    public string Name { get; }

    /// <summary> The type of the value held by the  <see cref="LIRMetadataValue"/>. </summary>
    public LIRType ValueType { get; }

    /// <summary> The value the  <see cref="LIRMetadataValue"/> is initialized with. </summary>
    public LIRValue Initializer { get; }

    /// <summary> Whether the  <see cref="LIRMetadataValue"/> holds constant memory that may not be written to. </summary>
    public bool IsConstant { get; }

    /// <inheritdoc />
    public override LIRType Type { get; }

    /// <summary> Initializes a new instance of the <see cref="LIRMetadataValue"/> class. </summary>
    private LIRMetadataValue(string name, LIRType valueType, LIRValue initializer, bool isConstant)
    {
        if (initializer.Type != valueType)
            throw new ArgumentException($"Metadata value '{name}' of type '{valueType}' cannot be initialized by a value of type '{initializer.Type}'.");

        Name = name;
        ValueType = valueType;
        Initializer = initializer;
        IsConstant = isConstant;
        Type = new LIRPointerType(valueType);
    }

    /// <summary> Creates a new <see cref="LIRMetadataValue"/> holding <paramref name="initializer"/>. </summary>
    public static LIRMetadataValue Define(string name, LIRType valueType, LIRValue initializer, bool isConstant = true) => new(name, valueType, initializer, isConstant);
}
