namespace Loom.Parser.AST;

/// <summary>
/// A version of <see cref="ASTVisitor"/> that automatically visits child nodes.
/// </summary>
public class ASTWalker : ASTVisitor
{
    /// <inheritdoc/>
    public override void Dispatch(ASTNode node)
    {
        var nodeType = node.GetType();
        base.Dispatch(node);

        if (cache.TryGetValue((GetType(), nodeType), out var method) && method != null)
            VisitChildren(node);
    }

    /// <inheritdoc/>
    protected override void OnUnhandled(ASTNode node) => VisitChildren(node);
}
