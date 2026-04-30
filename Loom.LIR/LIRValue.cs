
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

    /// <inheritdoc/>
    public override string ToString() => $"%{ID}";
}

public class LIRConstantIntValue : LIRValue
{
    public int Value { get; }

    /// <inheritdoc/>
    public override LIRType Type => LIRIntType.Int32;

    /// <summary> Initializes a new instance of the <see cref="LIRConstantIntValue"/> class. </summary>
    public LIRConstantIntValue(int value) => Value = value;

    /// <inheritdoc/>
    public override string ToString() => Value.ToString();
}