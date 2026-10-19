using System.Collections.Immutable;

namespace Loom.Analyzer.Symbols;

/// <summary>
/// A method paired with its own generic arguments and its containing type's substitution.
/// </summary>
public sealed class ConstructedMethodSymbol : SubstitutedMethodSymbol
{
    /// <inheritdoc/>
    public override MethodSymbol ConstructedFrom { get; }

    /// <inheritdoc/>
    public override IReadOnlyList<TypeSymbol> TypeArguments { get; }

    /// <summary> Initializes a semantic construction of <paramref name="definition"/>. </summary>
    internal ConstructedMethodSymbol(MethodSymbol definition, ImmutableArray<TypeSymbol> arguments) : base(definition, definition.Substitution.Then(TypeSubstitution.Create(definition, arguments)), definition.ContainingType)
    {
        ConstructedFrom = definition;
        TypeArguments = arguments;
    }
}
