using Loom.Parser.AST;
using Loom.Parser.AST.Rules;
using Loom.Parser.Tokenizer;
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
        // Snapshot the callee before parsing children
        // Nested calls reuse this rule instance and overwrite the field
        var target = callee;

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

    // hacky hacky fix now
    
    public static bool CanParse(TokenReader reader)
    {
        if (reader.Check(TokenType.LParen))
            return true;

        return reader.Peek(TypeArgumentListRule.SkipTypeArguments(reader, 0)).Type == TokenType.LParen;
    }
}

