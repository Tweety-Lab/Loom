
using Loom.LIR.Builders;
using Loom.LIR.Objects;

namespace Loom.LIR;

public abstract record LIRValue;

/// <summary> A reference to a named temporary e.g. %0, %x </summary>
public record LIRTempValue(int Id) : LIRValue
{
    /// <inheritdoc/>
    public override string ToString() => $"%{Id}";
}

public record LIRConstantValue(object Value) : LIRValue;

public record LIRFunctionValue(LIRFunction Function) : LIRValue
{
    /// <inheritdoc/>
    public override string ToString() => $"@{Function.Name}";
}