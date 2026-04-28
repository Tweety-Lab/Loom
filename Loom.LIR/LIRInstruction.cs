using Loom.LIR.OpCodes;

namespace Loom.LIR;

public record LIRInstruction(LIROpCode OpCode, List<LIRValue> Operands)
{
    /// <summary> The result, or null if this instruction produces no value. </summary>
    public LIRValue? Result { get; init; }
}
