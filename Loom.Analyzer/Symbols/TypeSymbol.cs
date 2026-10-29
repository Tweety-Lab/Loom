using System.Collections.Immutable;

namespace Loom.Analyzer.Symbols;

/// <summary> A type definition, type parameter, array, or constructed named type. </summary>
public class TypeSymbol : Symbol, IExportableSymbol, IGenericSymbol
{
    private readonly SymbolConstructionCache<ConstructedTypeSymbol> constructions = new();

    /// <summary> The built-in category used when lowering this type. </summary>
    public DefaultType KnownType { get; }

    /// <summary> The members declared by this type, or their substituted views. </summary>
    public virtual IReadOnlyList<Symbol> Members { get; internal set; } = [];

    /// <summary> The directly declared base class, or null when there is none. </summary>
    public virtual TypeSymbol? BaseType { get; internal set; }

    /// <summary> All interfaces declared directly on this type. </summary>
    public virtual IReadOnlyList<TypeSymbol> Interfaces { get; internal set; } = [];

    /// <summary> All interfaces, including those inherited from base classes. </summary>
    public IReadOnlyList<TypeSymbol> AllInterfaces
    {
        get
        {
            var result = new List<TypeSymbol>();
            var visited = new HashSet<TypeSymbol>();
            var path = new HashSet<TypeSymbol>();

            void Visit(TypeSymbol type)
            {
                if (!path.Add(type.OriginalDefinition))
                    return;

                if (visited.Add(type))
                {
                    if (type.BaseType is { } baseType)
                        Visit(baseType);

                    foreach (var implemented in type.Interfaces.Reverse())
                        Visit(implemented);

                    if (!ReferenceEquals(type, this) && type.KnownType == DefaultType.Interface)
                        result.Add(type);
                }

                path.Remove(type.OriginalDefinition);
            }

            Visit(this);
            result.Reverse();
            return result;
        }
    }

    /// <inheritdoc/>
    public virtual IReadOnlyList<TypeParameterSymbol> TypeParameters { get; internal set; } = [];

    /// <inheritdoc/>
    public virtual IReadOnlyList<TypeSymbol> TypeArguments => TypeParameters;

    /// <inheritdoc/>
    public int Arity => TypeParameters.Count;

    /// <inheritdoc/>
    public bool IsGeneric => Arity > 0;

    /// <inheritdoc/>
    public virtual bool ContainsTypeParameters => TypeArguments.Any(argument => argument.ContainsTypeParameters);

    /// <inheritdoc/>
    public override TypeSymbol OriginalDefinition => (TypeSymbol)base.OriginalDefinition;

    /// <inheritdoc/>
    public bool IsExported { get; internal set; }

    /// <summary> Whether this type uses value semantics. </summary>
    public bool IsValueType { get; internal set; }

    /// <summary> Whether this type can only exist on the stack. </summary>
    public bool IsStackOnly { get; internal set; }

    /// <summary> Whether this type is static. </summary>
    public bool IsStatic { get; internal set; }

    /// <summary> The semantic constructions requested for this definition, including open types. </summary>
    internal IEnumerable<ConstructedTypeSymbol> Constructions => constructions.Symbols;

    /// <summary> Initializes a type declaration. </summary>
    public TypeSymbol(string name, DefaultType knownType) : base(name) => KnownType = knownType;

    /// <summary> Constructs a named type without generating a layout or duplicating a body. </summary>
    public TypeSymbol Construct(params ImmutableArray<TypeSymbol> arguments)
    {
        TypeSubstitution.ValidateArity(OriginalDefinition, arguments.Length);

        if (arguments.SequenceEqual(OriginalDefinition.TypeParameters))
            return OriginalDefinition;

        return OriginalDefinition.constructions.GetOrCreate(arguments, arguments => new(OriginalDefinition, arguments));
    }

    /// <summary> Gets a declaration as a member of this type, including property accessors and backing fields. </summary>
    public virtual Symbol GetMember(Symbol definition) => definition is MemberSymbol member && ReferenceEquals(member.ContainingType?.OriginalDefinition, OriginalDefinition) ? definition.OriginalDefinition : throw new ArgumentException($"'{definition.Name}' does not belong to '{Name}'.", nameof(definition));

    /// <summary> Initializes the metadata shared by a definition and its constructed views. </summary>
    protected TypeSymbol(TypeSymbol definition) : base(definition)
    {
        KnownType = definition.KnownType;
        TypeParameters = definition.TypeParameters;
        IsValueType = definition.IsValueType;
        IsStackOnly = definition.IsStackOnly;
        IsStatic = definition.IsStatic;
        IsExported = definition.IsExported;
    }

    public enum DefaultType
    {
        Func,
        Void,
        Byte,
        I32,
        I64,
        IPtr,
        Bool,
        Char,
        Struct,
        Class,
        Interface,
        Enum,
        TypeParameter
    }
}
