using Loom.LIR.OpCodes;
using System.Reflection;

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

    public void EmitReturn(LIRValue? value = null)
    {
        if (value == null)
            Emit(LIROpCode.Return, null);
        else
            Emit(LIROpCode.Return, null, value);
    }

    public LIRTempValue EmitAdd(LIRValue left, LIRValue right) => Emit(LIROpCode.Add, left.Type, left, right);

    public LIRTempValue EmitLoad(LIRValue pointer)
    {
        var pointeeType = ((LIRPointerType)pointer.Type).PointeeType;
        return Emit(LIROpCode.Load, pointeeType, pointer);
    }

    public void EmitStore(LIRValue value, LIRValue address) => Emit(LIROpCode.Store, null, value, address);
    public LIRTempValue EmitAlloca(LIRType type) => Emit(LIROpCode.Alloca, new LIRPointerType(type));
    public LIRTempValue EmitCall(LIRFunction function, params LIRValue[] arguments)
    {
        LIRValue[] operands = [function, .. arguments];
        var returnType = function.Type.ReturnType;

        if (returnType == LIRType.Void)
        {
            WritingBlock.Emit(new LIRInstruction(LIROpCode.Call, operands.ToList()));
            return null;
        }

        return Emit(LIROpCode.Call, returnType, operands);
    }

    public LIRBasicBlock CreateBlock(string name)
    {
        var block = new LIRBasicBlock(name, function);
        function.Blocks.Add(block);
        return block;
    }

    public void SwitchTo(LIRBasicBlock block) => WritingBlock = block;

    public LIRTempValue EmitCmpEq(LIRValue left, LIRValue right) => Emit(LIROpCode.CmpEq, LIRType.Boolean, left, right);

    public void EmitCondBr(LIRValue condition, LIRBasicBlock trueTarget, LIRBasicBlock falseTarget) =>
        Emit(LIROpCode.CondBr, null, condition, new LIRBlockValue(trueTarget), new LIRBlockValue(falseTarget));

    public void EmitBr(LIRBasicBlock target) =>
        Emit(LIROpCode.Br, null, new LIRBlockValue(target));
}
