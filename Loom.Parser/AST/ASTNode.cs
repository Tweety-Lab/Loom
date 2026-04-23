
namespace Loom.Parser.AST;

/// <summary>
/// Base class for all nodes in the AST.
/// </summary>
public abstract record ASTNode
{
    /// <summary> Accepts a visitor. </summary>
    public abstract void Accept(ASTVisitor visitor);

    /// <summary> The children of this node. </summary>
    public abstract IEnumerable<ASTNode> Children { get; }
}
