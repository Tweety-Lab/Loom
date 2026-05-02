
using Loom.LIR.Objects;
using Loom.LIR.OpCodes;

namespace Loom.LIR;

public sealed class LIRBasicBlock
{
    /// <summary> The name of the block. </summary>
    public string Name { get; }

    /// <summary> The function that this block is contained in. </summary>
    public LIRFunction Parent { get; }

    /// <summary> All instructions in the block. </summary>
    public List<LIRInstruction> Instructions { get; } = new();

    /// <summary> The last instruction that ends control flow (ret/br/brcond) in the block. </summary>
    public LIRInstruction? Terminator { get; private set; }

    /// <summary> Initializes a new instance of the <see cref="LIRBasicBlock"/> class. </summary>
    public LIRBasicBlock(string name, LIRFunction function)
    {
        Name = name;
        Parent = function;
    }

    public void Add(LIRInstruction instruction)
    {
        if (Terminator is not null)
            throw new InvalidOperationException("Cannot add instruction after terminator.");

        Instructions.Add(instruction);

        if (instruction.OpCode.Type == LIROpCode.CodeType.Control)
            Terminator = instruction;
    }
}
