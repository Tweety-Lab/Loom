using static Loom.Parser.Tokenizer.Token;

namespace Loom.Parser.AST.Rules.Default;

public record InstanceCreationExpresssionNode(IdentifierNameNode TypeName, TypeArgumentListNode? TypeArguments, List<ExpressionNode> Arguments) : ExpressionNode
{
    /// <inheritdoc/>
    public override IEnumerable<ASTNode> Children => TypeArguments is null ? Arguments.Prepend(TypeName) : Arguments.Cast<ASTNode>().Prepend(TypeArguments).Prepend(TypeName);
}

[ParserRule]
public class InstanceCreationExpressionRule : ParserRule<InstanceCreationExpresssionNode>
{
    /// <inheritdoc/>
    public InstanceCreationExpressionRule(LoomParser parser) : base(parser) { }

    /// <inheritdoc/>
    public override InstanceCreationExpresssionNode ParseNode()
    {
        Parser.Reader.Expect(TokenType.New); // new
        var typeName = Parser.Reader.Expect(TokenType.Identifier); // name

        TypeArgumentListNode? typeArguments = null;
        if (Parser.Reader.Check(TokenType.Less))
            typeArguments = RunRule<TypeArgumentListRule, TypeArgumentListNode>();

        Parser.Reader.Expect(TokenType.LParen); // (

        // Arguments
        var args = new List<ExpressionNode>();
        if (Parser.Reader.Peek(0).Type != TokenType.RParen)
        {
            args.Add(Parser.GetRule<ExpressionRule>().Parse());

            while (Parser.Reader.Match(TokenType.Comma))
                args.Add(Parser.GetRule<ExpressionRule>().Parse());
        }

        Parser.Reader.Expect(TokenType.RParen); // )

        return new InstanceCreationExpresssionNode(new IdentifierNameNode(typeName), typeArguments, args);
    }
}

