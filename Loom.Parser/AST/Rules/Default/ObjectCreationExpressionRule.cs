using Loom.Parser.AST;
using Loom.Parser.AST.Rules;
using static Loom.Parser.Tokenizer.Token;

namespace Loom.Parser.AST.Rules.Default;

public record ObjectCreationExpressionNode(IdentifierNameNode ObjectName, List<ExpressionNode> Arguments) : ExpressionNode
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

        // Arguments
        var args = new List<ExpressionNode>();
        if (Parser.Reader.Peek(0).Type != TokenType.RParen)
        {
            args.Add(Parser.GetRule<ExpressionRule>().ParseNode());

            while (Parser.Reader.Match(TokenType.Comma))
                args.Add(Parser.GetRule<ExpressionRule>().ParseNode());
        }

        Parser.Reader.Expect(TokenType.RParen); // )

        return new ObjectCreationExpressionNode(new IdentifierNameNode(objectName), args);
    }
}

