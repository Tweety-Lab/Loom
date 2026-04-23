using Loom.Parser.Rules.Default;

namespace Loom.Parser.AST;

/// <summary>
/// A version of <see cref="ASTVisitor"/> that automatically visits child nodes.
/// </summary>
public class ASTWalker : ASTVisitor
{
    // ewwww...


    public override void Visit(BlockNode node)
    {
        base.Visit(node);

        foreach (var child in node.Children)
            child.Accept(this);
    }

    public override void Visit(ImportNode node)
    {
        base.Visit(node);

        foreach (var child in node.Children)
            child.Accept(this);
    }

    public override void Visit(ModuleNode node)
    {
        base.Visit(node);

        foreach (var child in node.Children)
            child.Accept(this);
    }

    public override void Visit(ProgramNode node)
    {
        base.Visit(node);

        foreach (var child in node.Children)
            child.Accept(this);
    }

    public override void Visit(UnsafeNode node)
    {
        base.Visit(node);

        foreach (var child in node.Children)
            child.Accept(this);
    }
}
