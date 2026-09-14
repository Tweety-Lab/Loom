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
/// To visit an AST node, declare a method taking that node type and mark it with <see cref="VisitorAttribute"/>. A method
/// taking a base node type (e.g. <see cref="StatementNode"/>) is matched for any of its subtypes, with the most derived
/// visitor taking precedence.
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
        var method = FindVisitMethod(GetType(), node.GetType());

        if (method != null)
            return method.Invoke(this, [node]);

        OnUnhandled(node);
        return null;
    }

    public virtual T? DispatchResult<T>(ASTNode node) => (T?)Dispatch(node);

    /// <summary> Finds the most specific visitor method for <paramref name="nodeType"/>, preferring exact matches over base types. </summary>
    protected static MethodInfo? FindVisitMethod(Type visitorType, Type nodeType)
    {
        return cache.GetOrAdd((visitorType, nodeType), key =>
            key.Item1.GetMethods()
                .Where(m => m.GetCustomAttribute<VisitorAttribute>() != null && m.GetParameters() is [var p] && p.ParameterType.IsAssignableFrom(key.Item2))
                .OrderBy(m => InheritanceDistance(key.Item2, m.GetParameters()[0].ParameterType))
                .FirstOrDefault());
    }

    /// <summary> The number of inheritance steps between <paramref name="from"/> and <paramref name="to"/>, or <see cref="int.MaxValue"/> if unrelated. </summary>
    private static int InheritanceDistance(Type from, Type to)
    {
        var distance = 0;
        var current = from;
        while (current != null && current != to)
        {
            current = current.BaseType;
            distance++;
        }
        return current == to ? distance : int.MaxValue;
    }
}
