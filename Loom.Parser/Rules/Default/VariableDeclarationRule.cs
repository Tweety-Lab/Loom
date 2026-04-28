using Loom.Parser.AST;
using Loom.Parser.Tokenizer;
using static Loom.Parser.Tokenizer.Token;

namespace Loom.Parser.Rules.Default;

public record VariableDeclarationNode(Token Type, IdentifierNameNode Name, ExpressionNode Initializer) : StatementNode
{
    public override IEnumerable<ASTNode> Children => [Initializer];
}

[ParserRule]
public class VariableDeclarationRule : ParserRule<VariableDeclarationNode>
{
    /// <inheritdoc/>
    public VariableDeclarationRule(LoomParser parser) : base(parser) { }

    /// <inheritdoc/>
    public override VariableDeclarationNode ParseNode()
    {
        var type = Parser.Reader.ExpectAny(TokenType.I32, TokenType.Void, TokenType.Identifier); // type
        var name = Parser.Reader.Expect(TokenType.Identifier); // name

        Parser.Reader.Expect(TokenType.Equals); // =

        var initializer = RunRule<ExpressionRule, ExpressionNode>(); // initializer
        return new VariableDeclarationNode(type, new IdentifierNameNode(name), initializer);
    }
}