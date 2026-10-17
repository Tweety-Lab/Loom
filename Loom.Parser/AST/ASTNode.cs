
using Loom.Parser.Tokenizer;
using System.Collections;

namespace Loom.Parser.AST;

/// <summary>
/// Base class for all nodes in the AST.
/// </summary>
public abstract record ASTNode
{
    /// <summary> The children of this node. </summary>
    public abstract IEnumerable<ASTNode> Children { get; }

    /// <summary> Every node below this node, outermost first. </summary>
    public IEnumerable<ASTNode> Descendants => Children.SelectMany(child => child.Descendants.Prepend(child));

    /// <summary> The first token that makes up this node. </summary>
    public Token? StartToken { get; set; }

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
    protected ListNode(IReadOnlyList<TNode> items) => Items = items;

    /// <inheritdoc/>
    public IEnumerator<TNode> GetEnumerator() => Items.GetEnumerator();

    /// <inheritdoc/>
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
