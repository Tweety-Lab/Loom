
using Loom.LIR.Generators;
using static System.Reflection.Metadata.BlobBuilder;

namespace Loom.LIR.Builders;

public sealed class LIRFunction
{
    public string Name { get; }
    public LIRFunctionType Signature { get; }
    public List<LIRBasicBlock> Blocks { get; }

    /// <summary> Initializes a new instance of the <see cref="LIRFunction"/> class. </summary>
    public LIRFunction(string name, LIRFunctionType signature, List<LIRBasicBlock> blocks)
    {
        Name = name;
        Signature = signature;
        Blocks = blocks;
    }
}

public class FunctionBuilder : ILIRBuilder<LIRFunction>
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
    public LIRFunction Build()
    {
        return new LIRFunction(Name, new LIRFunctionType(ReturnType, Parameters), Blocks);
    }

    /// <summary> Creates a new basic block. </summary>
    public LIRBasicBlock CreateBlock(string name)
    {
        var block = new LIRBasicBlock(name);
        Blocks.Add(block);
        return block;
    }
}
