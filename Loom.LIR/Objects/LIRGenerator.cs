using Loom.LIR.OpCodes;

namespace Loom.LIR.Objects;

/// <summary>
/// Handles writing of Loom Intermediate Representation (LIR) into <see cref="FunctionBuilder"/>s.
/// </summary>
public class LIRGenerator
{
    /// <summary> Returns the next available temporary register. </summary>
    public int NextTemp => currentTemp++;

    /// <summary> The current LIR writing block. </summary>
    public LIRBasicBlock WritingBlock
    {
        get => field;
        set
        {
            if (!function.Blocks.Contains(value))
                throw new ArgumentException($"Block '{value}' is not part of the function '{function.Name}'.");

            field = value;
        }
    }

    private int currentTemp = 0;
    private LIRFunction function;

    /// <summary> Initializes a new instance of the <see cref="LIRGenerator"/> class. </summary>
    public LIRGenerator(LIRFunction function)
    {
        this.function = function;
        LIRBasicBlock entry = new LIRBasicBlock("entry", function);
        function.Blocks.Add(entry);

        WritingBlock = entry;
    }

    /// <summary> Emits a <see cref="LIROpCode"/> to the current LIR stream. </summary>
    /// <param name="opCode"> The opcode to emit. </param>
    /// <param name="resultType"> The type of the result (if any). </param>
    /// <param name="operands"> The operands to emit. </param>
    /// <returns> The result of the emitted instruction or null if the instruction has no result. </returns>
    public LIRTempValue Emit(LIROpCode opCode, LIRType? resultType = null, params LIRValue[] operands)
    {
        LIRTempValue? result = null;

        if (opCode.HasResult && resultType == null)
            throw new ArgumentNullException(nameof(resultType));

        if (opCode.HasResult)
            result = new LIRTempValue(currentTemp++.ToString(), resultType);

        WritingBlock.Emit(new LIRInstruction(opCode, operands.ToList()) { Result = result });

        return result!;
    }

    public void EmitReturn(LIRValue? value = null) => Emit(LIROpCode.Return, null, value);
    public LIRTempValue EmitAdd(LIRValue left, LIRValue right) => Emit(LIROpCode.Add, left.Type, left, right);
    public LIRTempValue EmitLoad(LIRValue value) => Emit(LIROpCode.Load, value.Type, value);
    public LIRTempValue EmitStore(LIRValue value, LIRValue address) => Emit(LIROpCode.Store, null, value, address);
    public LIRTempValue EmitAlloca(LIRType type) => Emit(LIROpCode.Alloca, new LIRPointerType(type));
}
