
using Loom.Parser.Tokenizer;
using System.Collections;

namespace Loom.Parser.AST;

public readonly record struct Span(int Start, int End, Token? StartToken)
{
    public int Length => End - Start;

    /// <summary> Checks if <paramref name="position"/> is between <see cref="Start"/> and <see cref="End"/>. </summary>
    public bool Contains(int position) => position >= Start && position < End;

    public static Span FromToken(Token token) => new(token.Start, token.End, token);

    public static Span Cover(Span first, Span last) => new(first.Start, last.End, first.StartToken);

    public static Span Cover(IEnumerable<ASTNode> nodes)
    {
        using var iterator = nodes.GetEnumerator();

        if (!iterator.MoveNext())
            return default;

        var first = iterator.Current.Span;
        var last = first;

        while (iterator.MoveNext())
            last = iterator.Current.Span;

        return Cover(first, last);
    }
}

/// <summary>
/// Base class for all nodes in the AST.
/// </summary>
public abstract record ASTNode
{
    protected ASTNode() { }
    protected ASTNode(Span span) => Span = span;

    /// <summary> The children of this node. </summary>
    public abstract IEnumerable<ASTNode> Children { get; }

    /// <summary> Every node below this node, outermost first. </summary>
    public IEnumerable<ASTNode> Descendants => Children.SelectMany(child => child.Descendants.Prepend(child));

    /// <summary> The source range and first token that make up this node. </summary>
    public Span Span { get; set; }

    /// <summary> Accepts a visitor. </summary>
    public void Accept(ASTVisitor visitor) => visitor.Dispatch(this);
}

/// <summary>
/// An <see cref="ASTNode"/> that wraps an ordered list of <typeparamref name="TNode"/>s.
/// </summary>
public abstract record ListNode<TNode> : ASTNode, IReadOnlyList<TNode> where TNode : ASTNode
{
    /// <summary> The elements contained in this list. </summary>
    public IReadOnlyList<TNode> Items { get; }

    /// <inheritdoc/>
    public override IEnumerable<ASTNode> Children => Items;

    /// <inheritdoc/>
    public TNode this[int index] => Items[index];

    /// <inheritdoc/>
    public int Count => Items.Count;

    /// <summary> Initializes a new instance of the <see cref="ListNode{TNode}"/> class. </summary>
    protected ListNode(IReadOnlyList<TNode> items) : base(Span.Cover(items)) => Items = items;

    /// <inheritdoc/>
    public IEnumerator<TNode> GetEnumerator() => Items.GetEnumerator();

    /// <inheritdoc/>
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
