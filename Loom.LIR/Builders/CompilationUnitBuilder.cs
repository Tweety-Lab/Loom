using Loom.LIR.Objects;

namespace Loom.LIR.Builders;

public class CompilationUnitBuilder : ILIRBuilder<LIRCompilationUnit>
{
    public List<FunctionBuilder> Functions { get; } = new List<FunctionBuilder>();

    /// <inheritdoc/>
    public Dictionary<string, string> MetaData { get; } = new();

    public FunctionBuilder DefineFunction(string name, LIRType returnType, List<LIRType> parameters)
    {
        var function = new FunctionBuilder(name, returnType, parameters);
        Functions.Add(function);
        return function;
    }

    /// <inheritdoc />
    public LIRCompilationUnit Build()
    {
        return new LIRCompilationUnit(Functions.Select(f => f.Build()).ToList()) { MetaData = MetaData };
    }
}
