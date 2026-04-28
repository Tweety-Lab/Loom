
using Loom.LIR.Generators;

namespace Loom.LIR.Builders;

public sealed class LIRFunction
{
    public string Name { get; }
    public LIRFunctionType Signature { get; }
    public List<LIRInstruction> Instructions { get; }

    /// <summary> Initializes a new instance of the <see cref="LIRFunction"/> class. </summary>
    public LIRFunction(string name, LIRFunctionType signature, List<LIRInstruction> instructions)
    {
        Name = name;
        Signature = signature;
        Instructions = instructions;
    }
}

public class FunctionBuilder : ILIRBuilder<LIRFunction>
{
    /// <summary> Gets the underlying LIR generator for this function. </summary>
    public LIRGenerator LIRGenerator { get; } = new LIRGenerator();

    /// <summary> The name of the function. </summary>
    public string Name { get; }

    /// <summary> The return type of the function. </summary>
    public LIRType ReturnType { get; }

    /// <summary> The input parameters of the function. </summary>
    public List<LIRType> Parameters { get; }

    /// <summary> Initializes a new instance of the <see cref="FunctionBuilder"/> class. </summary>
    public FunctionBuilder(string name, LIRType returnType, List<LIRType> parameters)
    {
        Name = name;
        ReturnType = returnType;
        Parameters = parameters;
    }

    /// <inheritdoc/>
    public LIRFunction Build()
    {
        return new LIRFunction(Name, new LIRFunctionType(ReturnType, Parameters), LIRGenerator.Instructions.ToList());
    }
}
