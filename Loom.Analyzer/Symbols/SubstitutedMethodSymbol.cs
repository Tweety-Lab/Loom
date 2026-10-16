namespace Loom.Analyzer.Symbols;

/// <summary> A method signature viewed through a substitution; its source body remains on the definition. </summary>
public class SubstitutedMethodSymbol : MethodSymbol
{
    /// <inheritdoc/>
    public override TypeSubstitution Substitution { get; }

    /// <inheritdoc/>
    public override TypeSymbol? ReturnType
    {
        get => field ??= OriginalDefinition.ReturnType is { } type ? Substitution.Resolve(type) : null;
        internal set;
    }

    /// <inheritdoc/>
    public override IReadOnlyList<ParameterSymbol> Parameters
    {
        get => field ??= [.. OriginalDefinition.Parameters.Select(parameter => new SubstitutedParameterSymbol(parameter, Substitution))];
        internal set;
    }

    /// <summary> Views a method declaration through a constructed containing type. </summary>
    internal SubstitutedMethodSymbol(MethodSymbol definition, ConstructedTypeSymbol containingType) : this(definition, containingType.Substitution, containingType) { }

    /// <summary> Initializes a substituted signature with a combined generic environment. </summary>
    protected SubstitutedMethodSymbol(MethodSymbol definition, TypeSubstitution substitution, TypeSymbol? containingType) : base(definition, containingType) => Substitution = substitution;
}
