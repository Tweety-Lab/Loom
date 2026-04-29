using Loom.LIR.Objects;

namespace Loom.LIR.Builders;

public class CompilationUnitBuilder : ILIRBuilder<LIRCompilationUnit>
{
    public List<FunctionBuilder> Functions { get; } = new List<FunctionBuilder>();

    /// <inheritdoc/>
    public Dictionary<string, string> MetaData { get; } = new();

    /// <summary> Defines a new function in the <see cref="LIRCompilationUnit"/>. </summary>
    public FunctionBuilder DefineFunction(string name, LIRType returnType, List<LIRType> parameters)
    {
        var function = new FunctionBuilder(name, returnType, parameters);
        Functions.Add(function);
        return function;
    }

    /// <summary> Gets a <see cref="FunctionBuilder"/> by name. </summary>
    public FunctionBuilder GetFunction(string name) => Functions.First(f => f.Name == name);

    /// <inheritdoc />
    public LIRCompilationUnit Build()
    {
        return new LIRCompilationUnit(Functions.Select(f => f.Build()).ToList()) { MetaData = MetaData };
    }
}
