using System.Diagnostics.CodeAnalysis;
using static Loom.Parser.Tokenizer.Token;

namespace Loom.Parser.AST.Rules.Default;

/// <summary> Parses the body of a module, whose members can be structs or methods. </summary>
[ParserRule]
public class ModuleBlockRule : BlockRule
{
    /// <inheritdoc/>
    public ModuleBlockRule(LoomParser parser) : base(parser) { }

    /// <inheritdoc/>
    protected override bool TryParseMember([NotNullWhen(true)] out ASTNode? node)
    {
        var offset = TypeRule.SkipModifiers(Parser.Reader, 0);

        if (Parser.Reader.Peek(offset).Type == TokenType.Struct)
        {
            node = RunRule<StructDeclarationRule, ASTNode>();
            return true;
        }

        if (Parser.Reader.Peek(offset).Type == TokenType.Class)
        {
            node = RunRule<ClassDeclarationRule, ClassDeclarationNode>();
            return true;
        }

        if (Parser.Reader.Peek(offset).Type == TokenType.Interface)
        {
            node = RunRule<InterfaceDeclarationRule, ASTNode>();
            return true;
        }

        if (TypeRule.IsTypeName(Parser.Reader.Peek(offset).Type))
        {
            offset = TypeRule.SkipType(Parser.Reader, offset);

            if (Parser.Reader.Peek(offset).Type == TokenType.Identifier)
            {
                offset = TypeParameterListRule.SkipTypeParameters(Parser.Reader, offset + 1);

                if (Parser.Reader.Peek(offset).Type == TokenType.LParen)
                {
                    node = RunRule<MethodDeclarationRule, ASTNode>();
                    return true;
                }
            }
        }

        node = null;
        return false;
    }
}
