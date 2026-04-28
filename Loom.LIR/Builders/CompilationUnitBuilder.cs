
namespace Loom.LIR.Builders;

public class LIRCompilationUnit
{
    public List<LIRFunction> Functions { get; } = new List<LIRFunction>();

    /// <summary> Initializes a new instance of the <see cref="LIRCompilationUnit"/> class. </summary>
    public LIRCompilationUnit(List<LIRFunction> functions) => Functions = functions;
}

public class CompilationUnitBuilder : ILIRBuilder<LIRCompilationUnit>
{
    public List<FunctionBuilder> Functions { get; } = new List<FunctionBuilder>();

    public FunctionBuilder DefineFunction(string name, LIRType returnType, List<LIRType> parameters)
    {
        var function = new FunctionBuilder(name, returnType, parameters);
        Functions.Add(function);
        return function;
    }

    /// <inheritdoc />
    public LIRCompilationUnit Build()
    {
        return new LIRCompilationUnit(Functions.Select(f => f.Build()).ToList());
    }
}
