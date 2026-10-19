namespace Loom.Analyzer.Symbols;

/// <summary>
/// A fixed-size array.
/// </summary>
public sealed class ArrayTypeSymbol : TypeSymbol
{
    /// <summary> The element type. </summary>
    public TypeSymbol ElementType { get; }

    /// <inheritdoc/>
    public override bool ContainsTypeParameters => ElementType.ContainsTypeParameters;

    /// <summary> The size of the array. </summary>
    public int Size { get; }

    /// <summary> Reuses this array when substitution leaves its element type unchanged. </summary>
    internal ArrayTypeSymbol WithElementType(TypeSymbol elementType) => ReferenceEquals(elementType, ElementType) ? this : new(elementType, Size);

    /// <summary> Initializes an array type with its element type and fixed length. </summary>
    public ArrayTypeSymbol(TypeSymbol elementType, int size) : base($"{elementType.Name}[{size}]", elementType.KnownType)
    {
        ElementType = elementType;
        Size = size;
        IsValueType = true;
        FullyQualifiedName = $"{elementType.FullyQualifiedName}[{size}]";
    }
}

