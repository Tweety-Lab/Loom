using Loom.Parser.AST;
using static Loom.Parser.Tokenizer.Token;

namespace Loom.Parser.Rules.Default;

public record CallExpressionNode(IdentifierNameNode MethodName, List<ExpressionNode> Arguments) : ExpressionNode
{
    /// <inheritdoc/>
    public override IEnumerable<ASTNode> Children => Arguments.Prepend(MethodName);
}

[ParserRule]
public class CallExpressionRule : ParserRule<CallExpressionNode>
{
    /// <inheritdoc/>
    public CallExpressionRule(LoomParser parser) : base(parser) { }

    /// <inheritdoc/>
    public override CallExpressionNode ParseNode()
    {
        var callName = Parser.Reader.Expect(TokenType.Identifier); // name

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

        return new CallExpressionNode(new IdentifierNameNode(callName), args);
    }
}

