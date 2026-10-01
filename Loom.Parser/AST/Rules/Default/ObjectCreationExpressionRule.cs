using Loom.Parser.AST;
using Loom.Parser.AST.Rules;
using static Loom.Parser.Tokenizer.Token;

namespace Loom.Parser.AST.Rules.Default;

public record ObjectCreationExpressionNode(IdentifierNameNode ObjectName) : ExpressionNode
{
    /// <inheritdoc/>
    public override IEnumerable<ASTNode> Children => [ObjectName];
}

[ParserRule]
public class ObjectCreationExpressionRule : ParserRule<ObjectCreationExpressionNode>
{
    /// <inheritdoc/>
    public ObjectCreationExpressionRule(LoomParser parser) : base(parser) { }

    /// <inheritdoc/>
    public override ObjectCreationExpressionNode ParseNode()
    {
        Parser.Reader.Expect(TokenType.New); // new
        var objectName = Parser.Reader.Expect(TokenType.Identifier); // name

        Parser.Reader.Expect(TokenType.LParen); // (

        while (Parser.Reader.Current.Type != TokenType.RParen)
            Parser.Reader.Advance(); // TODO: Arguments

        Parser.Reader.Expect(TokenType.RParen); // )

        return new ObjectCreationExpressionNode(new IdentifierNameNode(objectName));
    }
}

