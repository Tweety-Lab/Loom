using System.Collections.Immutable;

namespace Loom.Analyzer.Symbols;

/// <summary> A named type definition paired with immutable, possibly open, generic arguments. </summary>
public sealed class ConstructedTypeSymbol : TypeSymbol
{
    private readonly Dictionary<Symbol, Symbol> members = [];

    /// <inheritdoc/>
    public override IReadOnlyList<TypeSymbol> TypeArguments { get; }

    /// <summary> The substitution from the original type's parameters to this type's arguments. </summary>
    public TypeSubstitution Substitution { get; }

    /// <inheritdoc/>
    public override IReadOnlyList<Symbol> Members
    {
        get => field ??= [.. OriginalDefinition.Members.Select(GetMember)];
        internal set;
    }

    /// <summary> Initializes a semantic construction of <paramref name="definition"/>. </summary>
    internal ConstructedTypeSymbol(TypeSymbol definition, ImmutableArray<TypeSymbol> arguments) : base(definition)
    {
        TypeArguments = arguments;
        Substitution = TypeSubstitution.Create(definition, arguments);
        FullyQualifiedName = $"{definition.FullyQualifiedName}<{string.Join(", ", arguments.Select(argument => argument.FullyQualifiedName))}>";
    }

    /// <inheritdoc/>
    public override Symbol GetMember(Symbol definition)
    {
        definition = base.GetMember(definition);

        if (!members.TryGetValue(definition, out var member))
            members.Add(definition, member = definition switch
            {
                MethodSymbol method => new SubstitutedMethodSymbol(method, this),
                FieldSymbol field => new SubstitutedFieldSymbol(field, this),
                PropertySymbol property => new SubstitutedPropertySymbol(property, this),
                _ => throw new ArgumentException($"'{definition.Name}' is not a type member.", nameof(definition))
            });

        return member;
    }
}
