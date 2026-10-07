
namespace Loom.Analyzer.Symbols;

// TODO: Remove this. At least move it to codegen.

/// <summary>
/// Substitutes the type parameters of a generic method with the concrete type arguments it was instantiated with.
/// </summary>
public sealed class TypeSubstitution
{
    private readonly Dictionary<string, TypeSymbol> bindings;

    /// <summary> Initializes a new instance of the <see cref="TypeSubstitution"/> class. </summary>
    public TypeSubstitution(IEnumerable<KeyValuePair<string, TypeSymbol>> bindings) => this.bindings = new Dictionary<string, TypeSymbol>(bindings);

    /// <summary> Creates a substitution binding every type parameter of <paramref name="method"/> to its corresponding type argument. </summary>
    public static TypeSubstitution Zip(MethodSymbol method, IReadOnlyList<TypeSymbol> typeArguments)
    {
        if (typeArguments.Count != method.TypeParameters.Count)
            throw new ArgumentException($"Expected {method.TypeParameters.Count} type argument(s) for {method.FullyQualifiedName}, got {typeArguments.Count}.");

        return new TypeSubstitution(method.TypeParameters.Select((parameter, index) => new KeyValuePair<string, TypeSymbol>(parameter.Name, typeArguments[index])));
    }

    /// <summary> Resolves every type parameter bound by this substitution within <paramref name="type"/> to its concrete type. </summary>
    public TypeSymbol Resolve(TypeSymbol type)
    {
        if (type is TypeParameterSymbol parameter && bindings.TryGetValue(parameter.Name, out TypeSymbol? bound))
            return bound;

        if (type is ArrayTypeSymbol array)
        {
            var element = Resolve(array.ElementType);
            return ReferenceEquals(element, array.ElementType) ? array : new ArrayTypeSymbol(element, array.Size);
        }

        return type;
    }
}
