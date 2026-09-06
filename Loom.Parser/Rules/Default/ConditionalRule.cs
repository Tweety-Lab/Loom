using Loom.Parser.AST;
using Loom.Parser.Tokenizer;
using static Loom.Parser.Tokenizer.Token;

namespace Loom.Parser.Rules.Default;

public record ConditionalNode(ExpressionNode Expression, BlockNode Body) : StatementNode
{
    /// <inheritdoc/>
    public override IEnumerable<ASTNode> Children => [Expression, Body];
}

[ParserRule]
public class ConditionalRule : ParserRule<ConditionalNode>
{
    /// <inheritdoc/>
    public ConditionalRule(LoomParser parser) : base(parser) { }

    /// <inheritdoc/>
    public override ConditionalNode ParseNode()
    {
        Parser.Reader.Expect(TokenType.If); // if

        Parser.Reader.Expect(TokenType.LParen); // (
        var condition = RunRule<ExpressionRule, ExpressionNode>();
        Parser.Reader.Expect(TokenType.RParen); // )

        var body = Parser.GetRule<MethodBlockRule>().ParseNode();

        return new ConditionalNode(condition, body);
    }
}
