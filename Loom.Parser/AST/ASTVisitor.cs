using System.Collections.Concurrent;
using System.Reflection;

namespace Loom.Parser.AST;

/// <summary>
/// Marks a method inside of an <see cref="ASTVisitor"/> as a visit method.
/// </summary>
/// <remarks>
/// Methods marked with this attribute must have a single parameter that is the <see cref="ASTNode"/> type it wants to visit.
/// </remarks>
[AttributeUsage(AttributeTargets.Method)]
public sealed class VisitorAttribute : Attribute { }

/// <summary>
/// An Abstract Syntax Tree visitor.
/// </summary>
/// <remarks>
/// To visit an AST node from a clas that inhe
/// 
/// This visitor requires manual visiting of children nodes. For automatic visiting, see <see cref="ASTWalker"/>.
/// </remarks>
public abstract class ASTVisitor
{
    protected static readonly ConcurrentDictionary<(Type visitor, Type node), MethodInfo?> cache = new();

    /// <summary> Visits all children of a node. </summary>
    public void VisitChildren(ASTNode node)
    {
        foreach (var child in node.Children)
            Dispatch(child);
    }

    /// <summary> Called when a node is dispatched that does not have a visit method. </summary>
    protected virtual void OnUnhandled(ASTNode node) { }

    /// <summary> Dispatches the node to the correct Visit method via reflection. </summary>
    /// <param name="node"> The node to dispatch. </param>
    /// <returns> The result of the dispatched method or null if it returns void. </returns>
    public virtual object? Dispatch(ASTNode node)
    {
        var method = cache.GetOrAdd((GetType(), node.GetType()), key => key.Item1.GetMethods().FirstOrDefault(m => m.GetCustomAttribute<VisitorAttribute>() != null && m.GetParameters() is [var p] && p.ParameterType == key.Item2));

        if (method != null)
            return method.Invoke(this, [node]);
        else
            OnUnhandled(node);

        return null;
    }

    public virtual T? DispatchResult<T>(ASTNode node) => (T?)Dispatch(node);
}
