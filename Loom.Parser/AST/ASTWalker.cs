using Loom.Parser.Rules.Default;

namespace Loom.Parser.AST;

/// <summary>
/// A version of <see cref="ASTVisitor"/> that automatically visits child nodes.
/// </summary>
public class ASTWalker : ASTVisitor
{
    protected void WalkChildren(ASTNode node)
    {
        foreach (var child in node.Children)
            Dispatch(child);
    }
}
