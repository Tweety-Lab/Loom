using Loom.Parser.AST.Rules.Default;
using Loom.Parser.Tokenizer;
using Loom.Parser.AST;
using static Loom.Parser.Tokenizer.Token;

namespace Loom.Parser.AST;

public abstract record NameNode : ExpressionNode;

public record IdentifierNameNode(Token Token) : NameNode
{
    public string BaseName => Token.Text;
}

public interface ITypeDeclarationNode : IModifiableNode
{
    Token Name { get; }
    BlockNode Body { get; }
}

/// <summary>
/// <see cref="ASTNode"/> that holds modifiers.
/// </summary>
public interface IModifiableNode
{
    /// <summary> All applied modifiers. </summary>
    List<Token> Modifiers { get; }
}

public static class ModifiableExtensions
{
    extension (IModifiableNode modifiable)
    {
        /// <summary> Returns true if the node has the specified modifier. </summary>
        public bool HasModifier(TokenType modifier) => modifiable.Modifiers.Any(m => m.Type == modifier);
    }
}
