namespace Loom.Parser.AST;

/// <summary>
/// A version of <see cref="ASTVisitor"/> that automatically visits child nodes.
/// </summary>
public class ASTWalker : ASTVisitor
{
    /// <inheritdoc/>
    public override object? Dispatch(ASTNode node)
    {
        var nodeType = node.GetType();

        if (cache.TryGetValue((GetType(), nodeType), out var method) && method != null)
            VisitChildren(node);

        return base.Dispatch(node);
    }

    /// <inheritdoc/>
    public override T? DispatchResult<T>(ASTNode node) where T : default
    {
        var nodeType = node.GetType();

        if (cache.TryGetValue((GetType(), nodeType), out var method) && method != null)
            VisitChildren(node);

        return base.DispatchResult<T>(node);
    }

    /// <inheritdoc/>
    protected override void OnUnhandled(ASTNode node) => VisitChildren(node);
}
