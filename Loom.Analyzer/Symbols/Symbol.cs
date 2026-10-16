using Loom.Parser.AST;

namespace Loom.Analyzer.Symbols;

/// <summary> A semantic declaration or a substituted view of one. Symbols have reference identity. </summary>
public abstract class Symbol(string name)
{
    /// <summary> The unqualified name of this symbol. </summary>
    public string Name { get; } = name;

    /// <summary> The declaration underlying a substituted symbol. </summary>
    protected Symbol? Definition { get; init; }

    /// <summary> The symbol as originally declared in source. </summary>
    public virtual Symbol OriginalDefinition => Definition?.OriginalDefinition ?? this;

    /// <summary> Whether this symbol is its original declaration. </summary>
    public bool IsDefinition => ReferenceEquals(this, OriginalDefinition);

    /// <summary> The fully qualified name of this symbol, i.e. "MyModule::MyMethod". </summary>
    public string FullyQualifiedName { get; set; } = name;

    /// <summary> The <see cref="ASTNode"/> that declared this symbol. </summary>
    public ASTNode? DeclaringNode { get; set; }

    /// <summary> Initializes a substituted view of <paramref name="definition"/>. </summary>
    protected Symbol(Symbol definition) : this(definition.Name)
    {
        Definition = definition.OriginalDefinition;
        FullyQualifiedName = definition.FullyQualifiedName;
        DeclaringNode = definition.DeclaringNode;
    }
}
