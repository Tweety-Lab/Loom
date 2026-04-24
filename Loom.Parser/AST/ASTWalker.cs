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
            child.Accept(this);
    }

    public override void Visit(MethodDefinitionNode node) => WalkChildren(node);
    public override void Visit(BlockNode node) => WalkChildren(node);
    public override void Visit(ImportNode node) => WalkChildren(node);
    public override void Visit(ModuleNode node) => WalkChildren(node);
    public override void Visit(ProgramNode node) => WalkChildren(node);
    public override void Visit(UnsafeNode node) => WalkChildren(node);
}
