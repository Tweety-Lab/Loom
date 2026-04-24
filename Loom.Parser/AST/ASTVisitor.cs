using Loom.Parser.Rules.Default;

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
    public virtual void Visit(MethodDefinitionNode node) { }
    public virtual void Visit(BlockNode node) { }
    public virtual void Visit(ImportNode node) { }
    public virtual void Visit(ModuleNode node) { }
    public virtual void Visit(ProgramNode node) { }
    public virtual void Visit(UnsafeNode node) { }
}
