
using Loom.Parser.AST;
using Loom.Parser.Tokenizer;
using static Loom.Parser.Tokenizer.Token;

namespace Loom.Parser.Rules;

/// <summary>
/// <see cref="ASTNode"/> that holds modifiers.
/// </summary>
public interface IModifiableNode
{
    /// <summary> All applied modifiers. </summary>
    List<Token> Modifiers { get; }
}

// Kinda hacky?
public static class ModifiableExtensions
{
    extension (IModifiableNode modifiable)
    {
        /// <summary> Returns true if the node has the specified modifier. </summary>
        public bool HasModifier(TokenType modifier) => modifiable.Modifiers.Any(m => m.Type == modifier);
    }
}
