
namespace Loom.LIR.Objects;

public sealed class LIRFunction : LIRValueObject
{
    public string Name { get; }
    public List<LIRBasicBlock> Blocks { get; }
    public IReadOnlyList<LIRValue> Parameters { get; }

    /// <inheritdoc />
    public override LIRFunctionType Type {  get; }

    /// <summary> Initializes a new instance of the <see cref="LIRFunction"/> class. </summary>
    public LIRFunction(string name, LIRFunctionType type, List<LIRBasicBlock> blocks)
    {
        Name = name;
        Type = type;
        Blocks = blocks;

        var parameters = new List<LIRValue>();
        foreach (var paramInfo in type.Parameters)
            parameters.Add(new LIRTempValue(paramInfo.Name, paramInfo.Type));

        Parameters = parameters;
    }

    /// <inheritdoc />
    public override string ToString() => $"@{Name}";
}
