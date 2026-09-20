using System.Diagnostics.CodeAnalysis;
using Loom.Parser.AST;
using Loom.Parser.Tokenizer;
using static Loom.Parser.Tokenizer.Token;

namespace Loom.Parser.Rules.Default;

/// <summary> Parses the body of a module, whose members can be structs or methods. </summary>
[ParserRule]
public class ModuleBlockRule : BlockRule
{
    /// <inheritdoc/>
    public ModuleBlockRule(LoomParser parser) : base(parser) { }

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

        if (Parser.Reader.Peek(offset).Type == TokenType.Class)
        {
            node = RunRule<ClassDeclarationRule, ClassDeclarationNode>();
            return true;
        }

        if (IsTypeName(Parser.Reader.Peek(offset).Type) && Parser.Reader.Peek(offset + 1).Type == TokenType.Identifier && Parser.Reader.Peek(offset + 2).Type == TokenType.LParen)
        {
            node = RunRule<MethodDeclarationRule, MethodDeclarationNode>();
            return true;
        }

        node = null;
        return false;
    }
}