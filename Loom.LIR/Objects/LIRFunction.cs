
namespace Loom.LIR.Objects;

public sealed class LIRFunction : LIRValueObject
{
    public string Name { get; }

    public List<LIRBasicBlock> Blocks { get; } = new List<LIRBasicBlock>();

    public IReadOnlyList<LIRValue> ParameterValues { get; }

    public LIRGenerator LIRGenerator { get; }

    /// <inheritdoc />
    public override LIRFunctionType Type {  get; }

    /// <summary> Initializes a new instance of the <see cref="LIRFunction"/> class. </summary>
    public LIRFunction(string name, LIRFunctionType type)
    {
        Name = name;
        Type = type;

        var parameters = new List<LIRValue>();
        foreach (var paramInfo in type.Parameters)
            parameters.Add(new LIRTempValue(paramInfo.Name, paramInfo.Type));

        ParameterValues = parameters;

        LIRGenerator = new LIRGenerator(this);
    }

    /// <inheritdoc />
    public override string ToString() => $"@{Name}";
}
