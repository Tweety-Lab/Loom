using Loom.Analyzer.Symbols;
using Loom.Parser.AST;
using Loom.Parser.Rules.Default;
using System;
using System.Collections.Generic;
using System.Text;

namespace Loom.Analyzer;

internal class ParentWalker : ASTVisitor
{
    /// <summary> All currently mapped <see cref="ASTNode"/> parents. </summary>
    public Dictionary<ASTNode, ASTNode> Parents { get; private set; }

    /// <summary> The current <see cref="ASTNode"/>. </summary>
    public ASTNode? Current { get; private set; }

    /// <summary> Initializes a new instance of the <see cref="TypeWalker"/> class. </summary>
    public ParentWalker(Dictionary<ASTNode, ASTNode> parents) => Parents = parents;

    /// <inheritdoc/>
    protected override void OnUnhandled(ASTNode node)
    {
        foreach (var child in node.Children)
        {
            Parents[child] = node;
            var previous = Current;
            Current = node;
            Dispatch(child);
            Current = previous;
        }
    }
}

