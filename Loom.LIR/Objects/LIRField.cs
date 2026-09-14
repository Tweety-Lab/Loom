
namespace Loom.LIR.Objects;

public sealed class LIRField : LIRValueObject
{
    public string Name { get; }

    /// <inheritdoc />
    public override LIRType Type { get; }

    /// <summary> Initializes a new instance of the <see cref="LIRField"/> class. </summary>
    private LIRField(string name, LIRType type)
    {
        Name = name;
        Type = type;
    }

    /// <summary> Creates a new <see cref="LIRField"/>. </summary>
    public static LIRField Declare(string name, LIRType type) => new(name, type);
}
