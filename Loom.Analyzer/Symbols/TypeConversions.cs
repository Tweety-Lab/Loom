using System.Runtime.CompilerServices;

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

        if (source is TypeParameterSymbol || target is TypeParameterSymbol)
            return true;

        if (source is ArrayTypeSymbol || target is ArrayTypeSymbol)
            return source is ArrayTypeSymbol { ElementType: var sourceElement, Size: var sourceSize }
                && target is ArrayTypeSymbol { ElementType: var targetElement, Size: var targetSize }
                && sourceSize == targetSize
                && CanImplicitlyConvert(sourceElement, targetElement);

        if (IsIdentical(source, target))
            return true;

        if (source.KnownType is TypeSymbol.DefaultType.Class or TypeSymbol.DefaultType.Struct ||
            target.KnownType is TypeSymbol.DefaultType.Class or TypeSymbol.DefaultType.Struct)
            return false;

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
    public static bool IsIdentical(TypeSymbol source, TypeSymbol target) => ReferenceEquals(source, target) || (source, target) switch
    {
        (ArrayTypeSymbol left, ArrayTypeSymbol right) => left.Size == right.Size && IsIdentical(left.ElementType, right.ElementType),
        (ConstructedTypeSymbol left, ConstructedTypeSymbol right) => ReferenceEquals(left.OriginalDefinition, right.OriginalDefinition) && left.TypeArguments.Count == right.TypeArguments.Count && left.TypeArguments.Zip(right.TypeArguments).All(pair => IsIdentical(pair.First, pair.Second)),
        _ => false
    };

    /// <summary> Hashes the same semantic identity used by <see cref="IsIdentical"/>. </summary>
    internal static int IdentityHashCode(TypeSymbol type) => type switch
    {
        ArrayTypeSymbol array => HashCode.Combine(IdentityHashCode(array.ElementType), array.Size),
        ConstructedTypeSymbol constructed => constructed.TypeArguments.Aggregate(RuntimeHelpers.GetHashCode(constructed.OriginalDefinition), (hash, argument) => HashCode.Combine(hash, IdentityHashCode(argument))),
        _ => RuntimeHelpers.GetHashCode(type)
    };

    private static bool IsIntegerType(TypeSymbol.DefaultType type) => type switch
    {
        TypeSymbol.DefaultType.I32 or TypeSymbol.DefaultType.I64 or TypeSymbol.DefaultType.IPtr or TypeSymbol.DefaultType.Char => true,
        _ => false
    };
}
