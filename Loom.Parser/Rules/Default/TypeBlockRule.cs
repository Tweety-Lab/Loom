using System.Diagnostics.CodeAnalysis;
using Loom.Parser.AST;
using Loom.Parser.Tokenizer;
using static Loom.Parser.Tokenizer.Token;

namespace Loom.Parser.Rules.Default;

/// <summary> Parses the body of a type, whose members can be methods, fields, or nested structs. </summary>
[ParserRule]
public class TypeBlockRule : BlockRule
{
    /// <inheritdoc/>
    public TypeBlockRule(LoomParser parser) : base(parser) { }

    /// <inheritdoc/>
    protected override bool TryParseMember([NotNullWhen(true)] out ASTNode? node)
    {
        var offset = 0;
        while (TokenRegistry.IsModifier(Parser.Reader.Peek(offset).Type))
            offset++;

        if (Parser.Reader.Peek(offset).Type == TokenType.Struct)
        {
            node = RunRule<StructDeclarationRule, StructDeclarationNode>();
            return true;
        }

        bool isTypeName = IsTypeName(Parser.Reader.Peek(offset).Type) && Parser.Reader.Peek(offset + 1).Type == TokenType.Identifier;

        if (isTypeName && Parser.Reader.Peek(offset + 2).Type == TokenType.LParen)
        {
            node = RunRule<MethodDeclarationRule, MethodDeclarationNode>();
            return true;
        }

        // A typed member followed by '=' is a field.
        if (isTypeName && Parser.Reader.Peek(offset + 2).Type == TokenType.Equals)
        {
            node = RunRule<FieldDeclarationRule, FieldDeclarationNode>();
            return true;
        }

        node = null;
        return false;
    }
}