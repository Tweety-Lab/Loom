
using Loom.Parser.Tokenizer;

namespace Loom.Parser.AST;

/// <summary>
/// Base class for all nodes in the AST.
/// </summary>
public abstract record ASTNode
{
    /// <summary> The children of this node. </summary>
    public abstract IEnumerable<ASTNode> Children { get; }

    /// <summary> The first token that makes up this node. </summary>
    public Token? StartToken { get; set; }

    /// <summary> Accepts a visitor. </summary>
    public void Accept(ASTVisitor visitor) => visitor.Dispatch(this);
}
