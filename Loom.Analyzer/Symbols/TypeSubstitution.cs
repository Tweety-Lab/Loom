using System.Collections.Immutable;

namespace Loom.Analyzer.Symbols;

/// <summary> An immutable generic environment keyed by declaration identity rather than parameter names. </summary>
public sealed class TypeSubstitution
{
    private readonly ImmutableDictionary<TypeParameterSymbol, TypeSymbol> bindings;

    /// <summary> The identity substitution. </summary>
    public static TypeSubstitution Empty { get; } = new([]);

    /// <summary> Initializes a substitution from explicit parameter bindings. </summary>
    public TypeSubstitution(IEnumerable<KeyValuePair<TypeParameterSymbol, TypeSymbol>> bindings) => this.bindings = bindings.ToImmutableDictionary();

    /// <summary> Binds the declaration's own type parameters to an ordered argument list. </summary>
    public static TypeSubstitution Create(IGenericSymbol definition, IReadOnlyList<TypeSymbol> arguments)
    {
        ValidateArity(definition, arguments.Count);

        return new(definition.TypeParameters.Select((parameter, index) => KeyValuePair.Create(parameter, arguments[index])));
    }

    /// <summary> Rejects argument lists that cannot describe a construction of this declaration. </summary>
    internal static void ValidateArity(IGenericSymbol definition, int count) => ArgumentOutOfRangeException.ThrowIfNotEqual(count, definition.Arity);

    /// <summary> Applies this environment followed by <paramref name="next"/>, including within existing bindings. </summary>
    public TypeSubstitution Then(TypeSubstitution next) => new(bindings.Select(binding => KeyValuePair.Create(binding.Key, next.Resolve(binding.Value))).ToImmutableDictionary().SetItems(next.bindings));

    /// <summary> Substitutes recursively through named types, generic arguments, and array elements. </summary>
    public TypeSymbol Resolve(TypeSymbol type) => type switch
    {
        TypeParameterSymbol parameter => bindings.GetValueOrDefault(parameter, parameter),
        ArrayTypeSymbol array => array.WithElementType(Resolve(array.ElementType)),
        { IsGeneric: true } => type.OriginalDefinition.Construct([.. type.TypeArguments.Select(Resolve)]),
        _ => type
    };

    /// <summary> Substitutes both the containing type and the method's own arguments. </summary>
    public MethodSymbol Resolve(MethodSymbol method)
    {
        var definition = method.ContainingType is { } type ? method.AsMemberOf(Resolve(type)) : method.ConstructedFrom;

        return method is ConstructedMethodSymbol ? definition.Construct([.. method.TypeArguments.Select(Resolve)]) : definition;
    }
}
