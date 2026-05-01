
namespace Loom.LIR;

public abstract class LIRValue
{
    /// <summary> The type of the <see cref="LIRValue"/>. </summary>
    public abstract LIRType Type { get; }
}

/// <summary> A reference to a named temporary e.g. %0, %x </summary>
public class LIRTempValue : LIRValue
{
    public string ID { get; }

    /// <inheritdoc/>
    public override LIRType Type { get; }

    /// <summary> Initializes a new instance of the <see cref="LIRTempValue"/> class. </summary>
    public LIRTempValue(string id, LIRType type)
    {
        ID = id;
        Type = type;
    }
}

public class LIRConstantIntValue : LIRValue
{
    public int Value { get; }

    /// <inheritdoc/>
    public override LIRType Type => LIRType.Int32;

    /// <summary> Initializes a new instance of the <see cref="LIRConstantIntValue"/> class. </summary>
    public LIRConstantIntValue(int value) => Value = value;
}

public class LIRConstantBoolValue : LIRValue
{
    public bool Value { get; }

    /// <inheritdoc/>
    public override LIRType Type => LIRType.Boolean;

    /// <summary> Initializes a new instance of the <see cref="LIRConstantBoolValue"/> class. </summary>
    public LIRConstantBoolValue(bool value) => Value = value;
}