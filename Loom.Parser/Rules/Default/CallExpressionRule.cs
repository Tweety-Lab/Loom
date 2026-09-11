using Loom.Parser.AST;
using static Loom.Parser.Tokenizer.Token;

namespace Loom.Parser.Rules.Default;

public record CallExpressionNode(ExpressionNode Callee, List<ExpressionNode> Arguments) : ExpressionNode
{
    /// <inheritdoc/>
    public override IEnumerable<ASTNode> Children => Arguments.Prepend(Callee);
}

[ParserRule]
public class CallExpressionRule : ParserRule<CallExpressionNode>
{
    /// <inheritdoc/>
    public CallExpressionRule(LoomParser parser) : base(parser) { }

    private ExpressionNode callee = null!;

    /// <summary> Parses a call on the given <paramref name="callee"/> expression. </summary>
    public CallExpressionNode Parse(ExpressionNode callee)
    {
        this.callee = callee;
        return Parse();
    }

    /// <inheritdoc/>
    public override CallExpressionNode ParseNode()
    {
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

        return new CallExpressionNode(callee, args);
    }
}

