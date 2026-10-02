using static Loom.Parser.Tokenizer.Token;

namespace Loom.Parser.AST.Rules.Default;

public record ArrayLiteralNode(List<ExpressionNode> Elements) : ExpressionNode
{
    /// <inheritdoc/>
    public override IEnumerable<ASTNode> Children => Elements;
}

[ParserRule]
public class ArrayLiteralExpressionRule : ParserRule<ArrayLiteralNode>
{
    /// <inheritdoc/>
    public ArrayLiteralExpressionRule(LoomParser parser) : base(parser) { }

    /// <inheritdoc/>
    public override ArrayLiteralNode ParseNode()
    {
        Parser.Reader.Expect(TokenType.LBracket); // [

        // elements
        var elements = new List<ExpressionNode>();
        if (Parser.Reader.Peek(0).Type != TokenType.RParen)
        {
            elements.Add(Parser.GetRule<ExpressionRule>().ParseNode());

            while (Parser.Reader.Match(TokenType.Comma))
                elements.Add(Parser.GetRule<ExpressionRule>().ParseNode());
        }

        Parser.Reader.Expect(TokenType.RBracket); // ]

        return new ArrayLiteralNode(elements);
    }
}
