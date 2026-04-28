using Loom.LIR.OpCodes;

namespace Loom.LIR.Generators;

public class LIRGenerator
{
    /// <summary> All currently emitted LIR instructions. </summary>
    public IReadOnlyList<LIRInstruction> Instructions => instructions;

    /// <summary> Returns the next available temporary register. </summary>
    public int NextTemp => currentTemp++;

    private List<LIRInstruction> instructions = new();
    private int currentTemp = 0;

    /// <summary> Emits a <see cref="LIROpCode"/> to the current LIR stream. </summary>
    /// <param name="opCode"> The opcode to emit. </param>
    /// <param name="operands"> The operands to emit. </param>
    /// <returns> The result of the emitted instruction or null if the instruction has no result. </returns>
    public LIRTempValue Emit(LIROpCode opCode, params LIRValue[] operands)
    {
        LIRTempValue? result = null;

        if (opCode.HasResult)
            result = new LIRTempValue(NextTemp);

        instructions.Add(new LIRInstruction(opCode, [.. operands]) { Result = result });
        return result!;
    }
}
