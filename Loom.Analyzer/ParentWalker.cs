using Loom.Parser.AST;

namespace Loom.Analyzer;

/// <summary>
/// Walks the AST and builds a map of every <see cref="ASTNode"/> to its parent, enabling ancestor traversal.
/// </summary>
internal class ParentWalker : ASTVisitor
{
    /// <summary> All currently mapped <see cref="ASTNode"/> parents. </summary>
    public Dictionary<ASTNode, ASTNode> Parents { get; private set; }

    /// <summary> The current <see cref="ASTNode"/>. </summary>
    public ASTNode? Current { get; private set; }

    /// <summary> Initializes a new instance of the <see cref="TypeWalker"/> class. </summary>
    public ParentWalker(Dictionary<ASTNode, ASTNode> parents) => Parents = parents;

    /// <inheritdoc/>
    protected override void OnUnhandled(ASTNode node)
    {
        foreach (var child in node.Children)
        {
            Parents[child] = node;
            var previous = Current;
            Current = node;
            Dispatch(child);
            Current = previous;
        }
    }
}

