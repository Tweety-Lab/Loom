using Loom.LIR.Builders;
using Loom.LIR.OpCodes;

namespace Loom.LIR.Generators;

/// <summary>
/// Handles writing of Loom Intermediate Representation (LIR) into <see cref="FunctionBuilder"/>s.
/// </summary>
public class LIRGenerator
{
    /// <summary> Returns the next available temporary register. </summary>
    public int NextTemp => currentTemp++;

    private int currentTemp = 0;
    private FunctionBuilder function;

    /// <summary> Initializes a new instance of the <see cref="LIRGenerator"/> class. </summary>
    public LIRGenerator(FunctionBuilder function) => this.function = function;

    /// <summary> Emits a <see cref="LIROpCode"/> to the current LIR stream. </summary>
    /// <param name="opCode"> The opcode to emit. </param>
    /// <param name="operands"> The operands to emit. </param>
    /// <returns> The result of the emitted instruction or null if the instruction has no result. </returns>
    public LIRTempValue Emit(LIROpCode opCode, params LIRValue[] operands)
    {
        var block = function.WritingBlock;

        LIRTempValue? result = null;

        if (opCode.HasResult)
            result = new LIRTempValue(currentTemp++);

        block.Instructions.Add(new LIRInstruction(opCode, operands.ToList()) { Result = result });

        return result!;
    }
}
