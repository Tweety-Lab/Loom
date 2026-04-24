using Loom.Parser.Rules.Default;
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
/// This visitor requires manual visiting of children nodes. For automatic visiting, see <see cref="ASTWalker"/>.
/// </remarks>
public abstract class ASTVisitor
{
    private static readonly ConcurrentDictionary<(Type visitor, Type node), MethodInfo?> cache = new();

    /// <summary> Called when a node is dispatched that does not have a visit method. </summary>
    protected virtual void OnUnhandled(ASTNode node) { }

    /// <summary> Dispatches the node to the correct Visit method via reflection. </summary>
    public void Dispatch(ASTNode node)
    {
        var method = cache.GetOrAdd((GetType(), node.GetType()), key => key.Item1.GetMethods().FirstOrDefault(m => m.GetCustomAttribute<VisitorAttribute>() != null && m.GetParameters() is [var p] && p.ParameterType == key.Item2));

        if (method != null)
            method.Invoke(this, [node]);
        else
            OnUnhandled(node);
    }
}
