namespace Loom.Analyzer.Symbols;

/// <summary>
/// A property whose field and accessors share the containing type's member projections.
/// </summary>
internal sealed class SubstitutedPropertySymbol(PropertySymbol definition, ConstructedTypeSymbol containingType) : PropertySymbol(definition, containingType)
{
    /// <inheritdoc/>
    public override PropertySymbol OriginalDefinition => (PropertySymbol)base.OriginalDefinition;

    /// <inheritdoc/>
    public override TypeSymbol? Type
    {
        get => field ??= OriginalDefinition.Type is { } type ? containingType.Substitution.Resolve(type) : null;
        internal set;
    }

    /// <inheritdoc/>
    public override FieldSymbol? BackingField
    {
        get => field ??= OriginalDefinition.BackingField is { } backingField ? (FieldSymbol)containingType.GetMember(backingField) : null;
        internal set;
    }

    /// <inheritdoc/>
    public override MethodSymbol? Getter
    {
        get => field ??= OriginalDefinition.Getter is { } getter ? getter.AsMemberOf(containingType) : null;
        internal set;
    }

    /// <inheritdoc/>
    public override MethodSymbol? Setter
    {
        get => field ??= OriginalDefinition.Setter is { } setter ? setter.AsMemberOf(containingType) : null;
        internal set;
    }
}
