
using Loom.LIR.Generators;

namespace Loom.LIR.Builders;

public record class LIRFunction(string Name, List<LIRInstruction> Instructions);

public class FunctionBuilder : LIRBuilder<LIRFunction>
{
    /// <summary> Gets the underlying LIR generator for this function. </summary>
    public LIRGenerator LIRGenerator { get; } = new LIRGenerator();

    /// <summary> The name of the function. </summary>
    public string Name { get; }

    /// <summary> Initializes a new instance of the <see cref="FunctionBuilder"/> class. </summary>
    public FunctionBuilder(string name) => Name = name;

    /// <inheritdoc/>
    public override LIRFunction Build()
    {
        return new LIRFunction(Name, LIRGenerator.Instructions.ToList());
    }
}
