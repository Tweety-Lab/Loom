using static Loom.Common.Diagnostics.DiagnosticDescription;
using Loom.Parser.Tokenizer;
using static Loom.Parser.Tokenizer.Token;
using Loom.Common.Diagnostics;

namespace Loom.Parser.AST.Rules.Default;

public record EnumMemberNode(Token Name, int? Value = null) : ASTNode(Span.FromToken(Name))
{
    /// <inheritdoc/>
    public override IEnumerable<ASTNode> Children => Enumerable.Empty<ASTNode>();
}

public record EnumDeclarationNode(Token Name, BlockNode Body, List<Token> Modifiers) : ASTNode, ITypeDeclarationNode
{
    /// <inheritdoc/>
    public override IEnumerable<ASTNode> Children => [Body];
}

[ParserRule]
public class EnumDeclarationRule : TypeMemberParserRule
{
    private static readonly DiagnosticDescription InvalidEnumValue = new($"LM{Code:D4}", Diagnostic.DiagnosticLevel.Error, "Enum values must be signed 32-bit integers.");

    /// <inheritdoc/>
    public EnumDeclarationRule(LoomParser parser) : base(parser) { }

    /// <inheritdoc/>
    public override EnumDeclarationNode ParseNode()
    {
        var modifiers = Parser.Reader.ExpectMany(t => TokenRegistry.IsMemberModifier(t.Type));

        Parser.Reader.Expect(TokenType.Enum);
        var name = Parser.Reader.Expect(TokenType.Identifier);
        var bodyStart = Parser.Reader.Position;
        Parser.Reader.Expect(TokenType.LBrace);

        var members = new List<ASTNode>();
        while (!Parser.Reader.Check(TokenType.RBrace))
        {
            var memberName = Parser.Reader.Expect(TokenType.Identifier);
            int? value = null;
            if (Parser.Reader.Match(TokenType.Equals))
            {
                var sign = Parser.Reader.Match(TokenType.Minus) ? "-" : "";
                var number = Parser.Reader.Expect(TokenType.Number);
                if (!int.TryParse(sign + number.Text, out var parsed))
                {
                    Parser.DiagnosticContext?.Report(InvalidEnumValue, number.Location);
                    throw new ParseFailureException(number, "Invalid enum value.");
                }
                value = parsed;
            }
            members.Add(new EnumMemberNode(memberName, value));

            if (!Parser.Reader.Check(TokenType.RBrace))
                Parser.Reader.Expect(TokenType.Comma);
        }

        Parser.Reader.Expect(TokenType.RBrace);
        return new EnumDeclarationNode(name, Parser.Complete(new BlockNode(members), bodyStart), modifiers);
    }
}
