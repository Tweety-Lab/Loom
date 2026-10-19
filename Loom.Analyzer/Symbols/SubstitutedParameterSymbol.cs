namespace Loom.Analyzer.Symbols;

/// <summary>
/// A parameter signature under a type or method substitution.
/// </summary>
internal sealed class SubstitutedParameterSymbol(ParameterSymbol definition, TypeSubstitution substitution) : ParameterSymbol(definition)
{
    /// <inheritdoc/>
    public override ParameterSymbol OriginalDefinition => (ParameterSymbol)base.OriginalDefinition;

    /// <inheritdoc/>
    public override TypeSymbol? Type
    {
        get => field ??= OriginalDefinition.Type is { } type ? substitution.Resolve(type) : null;
        internal set;
    }
}
