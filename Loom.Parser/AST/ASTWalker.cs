using Loom.Parser.Rules.Default;
using System.Reflection;

namespace Loom.Parser.AST;

/// <summary>
/// A version of <see cref="ASTVisitor"/> that automatically visits child nodes.
/// </summary>
public class ASTWalker : ASTVisitor
{
    /// <inheritdoc/>
    public override void Dispatch(ASTNode node)
    {
        var hasVisitor = cache.ContainsKey((GetType(), node.GetType()));
        base.Dispatch(node);

        if (hasVisitor)
            VisitChildren(node);
    }

    /// <inheritdoc/>
    protected override void OnUnhandled(ASTNode node) => VisitChildren(node);
}
