namespace Loom.LIR.Objects;

internal sealed class LIRVirtualMethodReference(int slot, LIRFunctionType type) : LIRValue
{
    public int Slot { get; } = slot;
    public override LIRFunctionType Type { get; } = type;
}