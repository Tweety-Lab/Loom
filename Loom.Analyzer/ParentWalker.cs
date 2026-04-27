using Loom.Parser.AST;

namespace Loom.Analyzer;

/// <summary>
/// Walks the AST and builds a map of every <see cref="ASTNode"/> to its parent, enabling ancestor traversal.
/// </summary>
internal class ParentWalker : ASTVisitor
{
    /// <summary> The owning <see cref="AnalysisContext"/>. </summary>
    public AnalysisContext Context { get; private set; }

    /// <summary> Initializes a new instance of the <see cref="TypeWalker"/> class. </summary>
    public ParentWalker(AnalysisContext context) => Context = context;

    /// <inheritdoc/>
    protected override void OnUnhandled(ASTNode node)
    {
        foreach (var child in node.Children)
        {
            Context.Parents[child] = node;
            Dispatch(child);
        }
    }
}

