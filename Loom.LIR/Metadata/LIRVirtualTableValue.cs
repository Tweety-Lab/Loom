using Loom.LIR.Objects;

namespace Loom.LIR.Metadata;

public sealed record LIRVirtualTableSlot(string Member, LIRFunction Implementation);

public sealed record LIRVirtualTableType(IReadOnlyList<LIRFunctionType> Slots) : LIRType;

public sealed class LIRVirtualTableValue : LIRValue
{
    public string ImplementingType { get; }
    public string InterfaceType { get; }
    public IReadOnlyList<LIRVirtualTableSlot> Slots { get; }
    public override LIRVirtualTableType Type { get; }

    public LIRVirtualTableValue(string implementingType, string interfaceType, IEnumerable<LIRVirtualTableSlot> slots)
    {
        ImplementingType = implementingType;
        InterfaceType = interfaceType;
        Slots = Array.AsReadOnly(slots.ToArray());

        if (Slots.Any(slot => slot.Implementation.Type.TypeParameters.Length != 0))
            throw new NotSupportedException("Generic interface methods require specialized dispatch slots.");

        Type = new(Array.AsReadOnly(Slots.Select(slot => slot.Implementation.Type).ToArray()));
    }
}
