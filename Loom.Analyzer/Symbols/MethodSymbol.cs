using System.Collections.Immutable;

namespace Loom.Analyzer.Symbols;

/// <summary> A method declaration or a substituted view of its signature. </summary>
public class MethodSymbol : MemberSymbol, IExportableSymbol, IGenericSymbol
{
    private readonly SymbolConstructionCache<ConstructedMethodSymbol> constructions = new();

    /// <summary> Whether this method is an ordinary method, constructor, or accessor. </summary>
    public MethodKind Kind { get; }

    /// <summary> The resolved return type. </summary>
    public virtual TypeSymbol? ReturnType { get; internal set; }

    /// <summary> The pointer type for the return value. </summary>
    public PointerType ReturnPointerType { get; set; }

    /// <inheritdoc/>
    public IReadOnlyList<TypeParameterSymbol> TypeParameters { get; internal set; } = [];

    /// <inheritdoc/>
    public virtual IReadOnlyList<TypeSymbol> TypeArguments => TypeParameters;

    /// <inheritdoc/>
    public int Arity => TypeParameters.Count;

    /// <inheritdoc/>
    public bool IsGeneric => Arity > 0;

    /// <inheritdoc/>
    public bool ContainsTypeParameters => ContainingType is { ContainsTypeParameters: true } || TypeArguments.Any(argument => argument.ContainsTypeParameters);

    /// <inheritdoc/>
    public override MethodSymbol OriginalDefinition => (MethodSymbol)base.OriginalDefinition;

    /// <summary> This method before its own type arguments were supplied, retaining containing-type substitution. </summary>
    public virtual MethodSymbol ConstructedFrom => this;

    /// <summary> The combined containing-type and method substitution for this signature. </summary>
    public virtual TypeSubstitution Substitution => TypeSubstitution.Empty;

    /// <summary> All parameters used by this method. </summary>
    public virtual IReadOnlyList<ParameterSymbol> Parameters { get; internal set; } = [];

    /// <summary> Whether this method is static. </summary>
    public bool IsStatic { get; set; }

    /// <summary> Whether this method is extern. </summary>
    public bool IsExtern { get; set; }

    /// <inheritdoc/>
    public bool IsExported { get; set; }

    /// <summary> Initializes a method declaration. </summary>
    public MethodSymbol(string name, MethodKind kind) : base(name) => Kind = kind;

    /// <summary> Constructs this method in its current containing type without generating a body. </summary>
    public MethodSymbol Construct(params ImmutableArray<TypeSymbol> arguments)
    {
        TypeSubstitution.ValidateArity(ConstructedFrom, arguments.Length);

        if (arguments.SequenceEqual(ConstructedFrom.TypeParameters))
            return ConstructedFrom;

        return ConstructedFrom.constructions.GetOrCreate(arguments, arguments => new(ConstructedFrom, arguments));
    }

    /// <summary> Gets this method as a member of <paramref name="type"/>. </summary>
    public MethodSymbol AsMemberOf(TypeSymbol type) => (MethodSymbol)type.GetMember(OriginalDefinition);

    /// <summary> Initializes the declaration metadata shared by substituted method signatures. </summary>
    protected MethodSymbol(MethodSymbol definition, TypeSymbol? containingType) : base(definition, containingType)
    {
        Kind = definition.Kind;
        TypeParameters = definition.TypeParameters;
        ReturnPointerType = definition.ReturnPointerType;
        IsStatic = definition.IsStatic;
        IsExtern = definition.IsExtern;
        IsExported = definition.IsExported;
    }

    public enum MethodKind
    {
        Constructor,
        Destructor,
        Accessor,
        Method
    }
}
