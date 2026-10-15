using Loom.Analyzer.Symbols;

namespace Loom.Analyzer;

/// <summary>
/// Creates concrete instantiations of generic types by binding their type parameters to type arguments.
/// </summary>
internal static class TypeInstantiator
{
    /// <summary>
    /// Instantiates <paramref name="definition"/> with <paramref name="typeArguments"/>, reusing a previously created
    /// instantiation when the same generic type was already instantiated with the same arguments.
    /// </summary>
    /// <param name="context"> The owning <see cref="AnalysisContext"/>. </param>
    /// <param name="definition"> The generic type to instantiate. </param>
    /// <param name="typeArguments"> The concrete type arguments to bind the type parameters to. </param>
    public static TypeSymbol Instantiate(AnalysisContext context, TypeSymbol definition, IReadOnlyList<TypeSymbol> typeArguments)
    {
        if (definition.TypeParameters.Count == 0 || definition.TypeParameters.Count != typeArguments.Count)
            return definition;

        // A generic type used inside another generic type is not instantiated until its own arguments are concrete
        if (typeArguments.Any(argument => argument.IsGeneric))
            return definition;

        string name = $"{definition.Name}<{string.Join(", ", typeArguments.Select(argument => argument.Name))}>";
        string fullyQualifiedName = $"{definition.FullyQualifiedName}<{string.Join(", ", typeArguments.Select(argument => argument.FullyQualifiedName))}>";

        if (context.InstantiatedTypes.TryGetValue(fullyQualifiedName, out var existing))
            return existing;

        var substitution = TypeSubstitution.Zip(definition, typeArguments);

        var instance = new InstantiatedTypeSymbol(definition, typeArguments, name, substitution)
        {
            FullyQualifiedName = fullyQualifiedName
        };

        context.InstantiatedTypes[fullyQualifiedName] = instance;
        context.Instantiations.Add(instance);

        return instance;
    }
}
