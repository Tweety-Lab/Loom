using static Loom.Parser.Tokenizer.Token;

namespace Loom.Parser.AST.Rules.Default;

public record CallExpressionNode(ExpressionNode Callee, TypeArgumentListNode TypeArguments, List<ExpressionNode> Arguments) : ExpressionNode
{
    /// <inheritdoc/>
    public override IEnumerable<ASTNode> Children => Arguments.Prepend(Callee);
}

[ParserRule]
public class CallExpressionRule : ParserRule<CallExpressionNode>
{
    /// <inheritdoc/>
    public CallExpressionRule(LoomParser parser) : base(parser) { }

    /// <summary> The receiver the call is being parsed on. Must be set before running this rule. </summary>
    public ExpressionNode Callee { get; set; } = null!;

    /// <inheritdoc/>
    public override CallExpressionNode ParseNode()
    {
        // Snapshot the callee before parsing children
        // Nested calls reuse this rule instance and overwrite the property
        var target = Callee;

        // Type Arguments
        var typeArguments = Parser.Reader.Check(TokenType.Less) ? RunRule<TypeArgumentListRule, TypeArgumentListNode>() : new TypeArgumentListNode([]);

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

        return new CallExpressionNode(target, typeArguments, args);
    }
}

