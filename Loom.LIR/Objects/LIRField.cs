
namespace Loom.LIR.Objects;

public sealed class LIRField : LIRValueObject
{
    public string Name { get; }

    /// <summary> The position of this field within its containing struct's layout. </summary>
    public int Index { get; }

    /// <inheritdoc />
    public override LIRType Type { get; }

    /// <summary> Initializes a new instance of the <see cref="LIRField"/> class. </summary>
    private LIRField(string name, int index, LIRType type)
    {
        Name = name;
        Index = index;
        Type = type;
    }

    /// <summary> Creates a new <see cref="LIRField"/>. </summary>
    public static LIRField Declare(string name, int index, LIRType type) => new(name, index, type);
}
