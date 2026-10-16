namespace Loom.Analyzer.Symbols;

/// <summary> A field viewed through the type arguments of its containing type. </summary>
internal sealed class SubstitutedFieldSymbol(FieldSymbol definition, ConstructedTypeSymbol containingType) : FieldSymbol(definition, containingType)
{
    /// <inheritdoc/>
    public override FieldSymbol OriginalDefinition => (FieldSymbol)base.OriginalDefinition;

    /// <inheritdoc/>
    public override TypeSymbol? Type
    {
        get => field ??= OriginalDefinition.Type is { } type ? containingType.Substitution.Resolve(type) : null;
        internal set;
    }
}
