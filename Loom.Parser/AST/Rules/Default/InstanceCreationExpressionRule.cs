using Loom.Parser.AST;
using Loom.Parser.AST.Rules;
using static Loom.Parser.Tokenizer.Token;

namespace Loom.Parser.AST.Rules.Default;

public record InstanceCreationExpressionNode(IdentifierNameNode TypeName, List<ExpressionNode> Arguments) : ExpressionNode
{
    /// <inheritdoc/>
    public override IEnumerable<ASTNode> Children => Arguments.Prepend(TypeName);
}

[ParserRule]
public class InstanceCreationExpressionRule : ParserRule<InstanceCreationExpressionNode>
{
    /// <inheritdoc/>
    public InstanceCreationExpressionRule(LoomParser parser) : base(parser) { }

    /// <inheritdoc/>
    public override InstanceCreationExpressionNode ParseNode()
    {
        Parser.Reader.Expect(TokenType.New); // new
        var typeName = Parser.Reader.Expect(TokenType.Identifier); // name

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

        return new InstanceCreationExpressionNode(new IdentifierNameNode(typeName), args);
    }
}

