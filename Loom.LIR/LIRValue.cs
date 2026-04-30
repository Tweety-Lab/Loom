
namespace Loom.LIR;

public abstract class LIRValue;

/// <summary> A reference to a named temporary e.g. %0, %x </summary>
public class LIRTempValue : LIRValue
{
    public string ID { get; }

    /// <summary> Initializes a new instance of the <see cref="LIRTempValue"/> class. </summary>
    public LIRTempValue(string id) => ID = id;

    /// <inheritdoc/>
    public override string ToString() => $"%{ID}";
}

public class LIRConstantIntValue : LIRValue
{
    public int Value { get; }

    /// <summary> Initializes a new instance of the <see cref="LIRConstantIntValue"/> class. </summary>
    public LIRConstantIntValue(int value) => Value = value;

    /// <inheritdoc/>
    public override string ToString() => Value.ToString();
}