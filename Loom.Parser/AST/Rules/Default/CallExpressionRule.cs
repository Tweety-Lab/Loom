using Loom.Parser.AST;
using Loom.Parser.AST.Rules;
using Loom.Parser.Tokenizer;
using static Loom.Parser.Tokenizer.Token;

namespace Loom.Parser.AST.Rules.Default;

public record CallExpressionNode(ExpressionNode Callee, List<TypeNode> TypeArguments, List<ExpressionNode> Arguments) : ExpressionNode
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
        var typeArgs = new List<TypeNode>();

        // Type Arguments
        if (Parser.Reader.Match(TokenType.Less)) // <
        {
            if (Parser.Reader.Peek(0).Type != TokenType.Greater)
            {
                do
                {
                    var type = RunRule<TypeRule, TypeNode>();
                    typeArgs.Add(type);
                }
                while (Parser.Reader.Match(TokenType.Comma));
            }

            Parser.Reader.Expect(TokenType.Greater); // >
        }

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

        return new CallExpressionNode(target, typeArgs, args);
    }

    // hacky hacky fix now
    
    public static bool CanParse(TokenReader reader)
    {
        if (reader.Check(TokenType.LParen))
            return true;

        return reader.Peek(SkipTypeArguments(reader, 0)).Type == TokenType.LParen;
    }

    public static int SkipTypeArguments(TokenReader reader, int offset)
    {
        if (reader.Peek(offset).Type != TokenType.Less)
            return offset;

        var start = offset;
        offset++;

        while (TypeRule.IsTypeName(reader.Peek(offset).Type))
        {
            offset = TypeRule.SkipType(reader, offset);

            if (reader.Peek(offset).Type != TokenType.Comma)
                break;

            offset++;
        }

        if (reader.Peek(offset).Type != TokenType.Greater)
            return start;

        return offset + 1;
    }
}

