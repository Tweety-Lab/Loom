using System.Diagnostics.CodeAnalysis;
using Loom.Parser.AST;
using Loom.Parser.Tokenizer;
using static Loom.Parser.Tokenizer.Token;

namespace Loom.Parser.AST.Rules.Default;

/// <summary>
/// Parses the body of a type, whose members can be methods, fields, or nested structs.
/// </summary>
[ParserRule]
public class TypeDeclarationBlockRule : BlockRule
{
    /// <inheritdoc/>
    public TypeDeclarationBlockRule(LoomParser parser) : base(parser) { }

    /// <inheritdoc/>
    protected override bool TryParseMember([NotNullWhen(true)] out ASTNode? node)
    {
        var offset = TypeRule.SkipModifiers(Parser.Reader, 0);
        var token = Parser.Reader.Peek(offset).Type;

        if (token == TokenType.Struct)
        {
            node = RunRule<StructDeclarationRule, StructDeclarationNode>();
            return true;
        }

        if (token == TokenType.Identifier && Parser.Reader.Peek(offset + 1).Type == TokenType.LParen)
        {
            node = RunRule<ConstructorDeclarationRule, ConstructorDeclarationNode>();
            return true;
        }

        if (!TypeRule.IsTypeName(token))
        {
            node = null;
            return false;
        }

        offset = TypeRule.SkipType(Parser.Reader, offset);

        if (Parser.Reader.Peek(offset).Type != TokenType.Identifier)
        {
            node = null;
            return false;
        }

        var next = Parser.Reader.Peek(offset + 1).Type;

        if (next == TokenType.Less)
        {
            offset = MethodDeclarationRule.SkipTypeParameters(Parser.Reader, offset + 1);
            next = Parser.Reader.Peek(offset).Type;
        }

        if (next == TokenType.LParen)
        {
            node = RunRule<MethodDeclarationRule, MethodDeclarationNode>();
            return true;
        }

        // A typed member followed by '=' is a field.
        if (next == TokenType.Equals)
        {
            node = RunRule<FieldDeclarationRule, FieldDeclarationNode>();
            return true;
        }

        node = null;
        return false;
    }
}