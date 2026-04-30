using Loom.LIR.Generators;
using Loom.LIR.Objects;

namespace Loom.LIR.Builders;

public class FunctionBuilder : LIRBuilder<LIRFunction>
{
    /// <summary> The name of the function. </summary>
    public string Name { get; }

    /// <summary> The return type of the function. </summary>
    public LIRType ReturnType { get; }

    /// <summary> The input parameters of the function. </summary>
    public List<LIRType> Parameters { get; }

    /// <summary> The current LIR generator. </summary>
    public LIRGenerator LIRGenerator { get; }

    /// <summary> The basic blocks of the function. </summary>
    public List<LIRBasicBlock> Blocks { get; } = new();

    /// <summary> The current writing basic block. </summary>
    public LIRBasicBlock WritingBlock { get; set; }

    /// <summary> Initializes a new instance of the <see cref="FunctionBuilder"/> class. </summary>
    public FunctionBuilder(string name, LIRType returnType, List<LIRType> parameters)
    {
        Name = name;
        ReturnType = returnType;
        Parameters = parameters;

        WritingBlock = CreateBlock("entry");
        LIRGenerator = new LIRGenerator(this);
    }

    /// <inheritdoc/>
    public override LIRFunction Rebuild() => new LIRFunction(Name, new LIRFunctionType(ReturnType, Parameters), Blocks) { MetaData = MetaData };

    /// <summary> Creates a new basic block. </summary>
    public LIRBasicBlock CreateBlock(string name)
    {
        var block = new LIRBasicBlock(name);
        Blocks.Add(block);
        return block;
    }
}
