using Loom.Parser.Rules.Default;
using System.Collections.Concurrent;
using System.Reflection;

namespace Loom.Parser.AST;

// TODO: Find a way to make the nodes own the visit call definitions

/// <summary>
/// An Abstract Syntax Tree visitor.
/// </summary>
/// <remarks>
/// This visitor requires manual visiting of children nodes. For automatic visiting, see <see cref="ASTWalker"/>.
/// </remarks>
public abstract class ASTVisitor
{
    private static readonly ConcurrentDictionary<(Type visitor, Type node), MethodInfo?> cache = new();

    /// <summary> Dispatches the node to the correct Visit method via reflection. </summary>
    public void Dispatch(ASTNode node)
    {
        var method = cache.GetOrAdd((GetType(), node.GetType()), key =>
            key.Item1.GetMethods()
                .FirstOrDefault(m =>
                    m.Name == "Visit" &&
                    !m.IsAbstract &&
                    m.GetParameters() is [var p] &&
                    p.ParameterType == key.Item2));

        method?.Invoke(this, [node]);
    }
}
