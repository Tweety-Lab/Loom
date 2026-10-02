using Loom.Parser.AST.Rules.Default;
using Loom.Parser.Tokenizer;
using Loom.Parser.AST;
using static Loom.Parser.Tokenizer.Token;

namespace Loom.Parser.AST;

/// <summary>
/// An <see cref="ASTNode"/> that names an entity, such as a variable, type, module or type parameter.
/// </summary>
public interface INameNode
{
    /// <summary> The text this name refers to. </summary>
    string BaseName { get; }
}

public record IdentifierNameNode(Token Token) : ExpressionNode, INameNode
{
    /// <inheritdoc/>
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
