
namespace Loom.LIR.Builders;

public record class LIRCompilationUnit(List<LIRFunction> Functions);

public class CompilationUnitBuilder : LIRBuilder<LIRCompilationUnit>
{
    public List<FunctionBuilder> Functions { get; } = new List<FunctionBuilder>();

    public FunctionBuilder DefineFunction(string name)
    {
        var function = new FunctionBuilder(name);
        Functions.Add(function);
        return function;
    }

    /// <inheritdoc />
    public override LIRCompilationUnit Build()
    {
        return new LIRCompilationUnit(Functions.Select(f => f.Build()).ToList());
    }
}
