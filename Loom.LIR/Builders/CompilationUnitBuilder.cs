using Loom.LIR.Objects;

namespace Loom.LIR.Builders;

public class CompilationUnitBuilder : LIRBuilder<LIRCompilationUnit>
{
    /// <inheritdoc cref="LIRCompilationUnit.Functions"/>
    public List<FunctionBuilder> Functions { get; } = new List<FunctionBuilder>();

    /// <summary> Defines a new function in the <see cref="LIRCompilationUnit"/>. </summary>
    public FunctionBuilder DefineFunction(string name, LIRType returnType, List<LIRParameter> parameters)
    {
        var function = new FunctionBuilder(name, returnType, parameters);
        Functions.Add(function);
        return function;
    }

    /// <summary> Gets a <see cref="FunctionBuilder"/> by name. </summary>
    public FunctionBuilder GetFunction(string name) => Functions.First(f => f.Name == name);

    /// <inheritdoc />
    public override LIRCompilationUnit Rebuild() => new LIRCompilationUnit(Functions.Select(f => f.BuildResult).ToList()) { MetaData = MetaData };
}
