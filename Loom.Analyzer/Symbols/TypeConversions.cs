namespace Loom.Analyzer.Symbols;

/// <summary>
/// The implicit conversions between <see cref="TypeSymbol"/>s.
/// </summary>
public static class TypeConversions
{
    /// <summary> Whether a value of type <paramref name="source"/> can be used where <paramref name="target"/> is expected. </summary>
    public static bool CanImplicitlyConvert(TypeSymbol? source, TypeSymbol? target)
    {
        if (source == null || target == null)
            return false;

        if (source.IsGeneric || target.IsGeneric)
            return true;

        // Ugly
        if (source is ArrayTypeSymbol sourceArray || target is ArrayTypeSymbol)
            return source is ArrayTypeSymbol { ElementType: var sourceElement, Size: var sourceSize }
                && target is ArrayTypeSymbol { ElementType: var targetElement, Size: var targetSize }
                && sourceSize == targetSize
                && CanImplicitlyConvert(sourceElement, targetElement);

        if (source.KnownType == target.KnownType)
            return true;

        if (target.KnownType == TypeSymbol.DefaultType.Void)
            return false;

        if (source.KnownType == TypeSymbol.DefaultType.Void)
            return false;

        // Casting check here

        if (IsIntegerType(source.KnownType) && IsIntegerType(target.KnownType))
            return true;

        return false;
    }

    /// <summary> Whether two types are the same type, rather than merely convertible between one another. </summary>
    public static bool IsIdentical(TypeSymbol source, TypeSymbol target) => ReferenceEquals(source, target) || source.LinkageName == target.LinkageName;

    private static bool IsIntegerType(TypeSymbol.DefaultType type) => type switch
    {
        TypeSymbol.DefaultType.I32 or TypeSymbol.DefaultType.I64 or TypeSymbol.DefaultType.IPtr or TypeSymbol.DefaultType.Char => true,
        _ => false
    };
}
