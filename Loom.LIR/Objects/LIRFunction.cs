
namespace Loom.LIR.Objects;

public sealed class LIRFunction : LIRValueObject
{
    public string Name { get; }
    public List<LIRBasicBlock> Blocks { get; }

    /// <inheritdoc />
    public override LIRFunctionType Type {  get; }

    /// <summary> Initializes a new instance of the <see cref="LIRFunction"/> class. </summary>
    public LIRFunction(string name, LIRFunctionType type, List<LIRBasicBlock> blocks)
    {
        Name = name;
        Type = type;
        Blocks = blocks;
    }

    /// <inheritdoc />
    public override string ToString() => $"@{Name}";
}
