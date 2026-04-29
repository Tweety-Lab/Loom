
namespace Loom.LIR;

public abstract class LIRValue;

/// <summary> A reference to a named temporary e.g. %0, %x </summary>
public class LIRTempValue : LIRValue
{
    public int Id { get; }

    /// <summary> Initializes a new instance of the <see cref="LIRTempValue"/> class. </summary>
    public LIRTempValue(int id) => Id = id;

    /// <inheritdoc/>
    public override string ToString() => $"%{Id}";
}

public class LIRConstantValue : LIRValue
{
    public object Value { get; }

    /// <summary> Initializes a new instance of the <see cref="LIRConstantValue"/> class. </summary>
    public LIRConstantValue(object value) => Value = value;
}