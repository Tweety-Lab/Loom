using System.Reflection;

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

        var method = cache.GetOrAdd((GetType(), nodeType), key => key.Item1.GetMethods().FirstOrDefault(m => m.GetCustomAttribute<VisitorAttribute>() != null && m.GetParameters() is [var p] && p.ParameterType == key.Item2));

        if (method != null)
            VisitChildren(node);

        return base.Dispatch(node);
    }

    /// <inheritdoc/>
    public override T? DispatchResult<T>(ASTNode node) where T : default
    {
        var nodeType = node.GetType();

        var method = cache.GetOrAdd((GetType(), nodeType), key => key.Item1.GetMethods().FirstOrDefault(m => m.GetCustomAttribute<VisitorAttribute>() != null && m.GetParameters() is [var p] && p.ParameterType == key.Item2));

        if (method != null)
            VisitChildren(node);

        return base.DispatchResult<T>(node);
    }

    /// <inheritdoc/>
    protected override void OnUnhandled(ASTNode node) => VisitChildren(node);
}