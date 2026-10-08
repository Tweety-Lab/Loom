
namespace Loom.LIR;

public abstract class LIRValue
{
    /// <summary> The type of the <see cref="LIRValue"/>. </summary>
    public abstract LIRType Type { get; }
}

/// <summary> A reference to a named temporary e.g. %0, %x </summary>
public class LIRTempValue : LIRValue
{
    public string ID { get; }

    /// <inheritdoc/>
    public override LIRType Type { get; }

    /// <summary> Initializes a new instance of the <see cref="LIRTempValue"/> class. </summary>
    public LIRTempValue(string id, LIRType type)
    {
        ID = id;
        Type = type;
    }
}

public class LIRConstantIntValue : LIRValue
{
    public int Value { get; }

    /// <inheritdoc/>
    public override LIRType Type => LIRType.Int32;

    /// <summary> Initializes a new instance of the <see cref="LIRConstantIntValue"/> class. </summary>
    public LIRConstantIntValue(int value) => Value = value;
}

/// <summary> A single Unicode scalar value, e.g. U+0041 ('A'). </summary>
public class LIRConstantCharValue : LIRValue
{
    /// <summary> The Unicode scalar value. </summary>
    public int Value { get; }

    /// <inheritdoc/>
    public override LIRType Type => LIRType.Char;

    /// <summary> Initializes a new instance of the <see cref="LIRConstantCharValue"/> class. </summary>
    public LIRConstantCharValue(int value) => Value = value;
}

/// <summary> A constant array of values, e.g. the initializer of a global. </summary>
public class LIRConstantArrayValue : LIRValue
{
    /// <summary> The elements of the array. </summary>
    public IReadOnlyList<LIRValue> Elements { get; }

    /// <inheritdoc/>
    public override LIRType Type { get; }

    /// <summary> Initializes a new instance of the <see cref="LIRConstantArrayValue"/> class. </summary>
    public LIRConstantArrayValue(LIRType elementType, IReadOnlyList<LIRValue> elements)
    {
        Elements = elements;
        Type = new LIRArrayType(elementType, elements.Count);
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj)
    {
        if (obj is not LIRConstantArrayValue other || Type != other.Type || Elements.Count != other.Elements.Count)
            return false;

        for (int index = 0; index < Elements.Count; index++)
            if (!AreEqual(Elements[index], other.Elements[index]))
                return false;

        return true;
    }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        HashCode hash = new HashCode();
        hash.Add(Type);

        foreach (var element in Elements)
            hash.Add(ElementKey(element));

        return hash.ToHashCode();
    }

    // Constants compare by value; anything else falls back to identity
    private static bool AreEqual(LIRValue left, LIRValue right) => ElementKey(left).Equals(ElementKey(right));

    private static object ElementKey(LIRValue element) => element switch
    {
        LIRConstantArrayValue array => array,
        LIRConstantCharValue character => character.Value,
        LIRConstantIntValue integer => integer.Value,
        LIRConstantBoolValue boolean => boolean.Value,
        _ => element
    };
}

public class LIRConstantBoolValue : LIRValue
{
    public bool Value { get; }

    /// <inheritdoc/>
    public override LIRType Type => LIRType.Boolean;

    /// <summary> Initializes a new instance of the <see cref="LIRConstantBoolValue"/> class. </summary>
    public LIRConstantBoolValue(bool value) => Value = value;
}

/// <summary> A null pointer, i.e. the default value of a reference type. </summary>
public class LIRNullValue : LIRValue
{
    /// <inheritdoc/>
    public override LIRType Type { get; }

    /// <summary> Initializes a new instance of the <see cref="LIRNullValue"/> class. </summary>
    /// <param name="type"> The type of the pointer this null value has. </param>
    public LIRNullValue(LIRType type) => Type = type;
}

/// <summary> The default (zero) value of an arbitrary type, e.g. the value of default(T). </summary>
public class LIRDefaultValue : LIRValue
{
    /// <inheritdoc/>
    public override LIRType Type { get; }

    /// <summary> Initializes a new instance of the <see cref="LIRDefaultValue"/> class. </summary>
    /// <param name="type"> The type this default value has. </param>
    public LIRDefaultValue(LIRType type) => Type = type;
}

public class LIRBlockValue : LIRValue
{
    public LIRBasicBlock Block { get; }

    /// <inheritdoc/>
    public override LIRType Type => LIRType.Void;

    /// <summary> Initializes a new instance of the <see cref="LIRBlockValue"/> class. </summary>
    public LIRBlockValue(LIRBasicBlock block) => Block = block;
}

/// <summary> A type used as an instruction operand for operations directed by a type rather than a value, i.e. sizeof. </summary>
public class LIRTypeValue(LIRType type) : LIRValue
{
    /// <inheritdoc/>
    public override LIRType Type { get; } = type;
}