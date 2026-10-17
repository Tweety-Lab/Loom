using Loom.Parser.Tokenizer;
using static Loom.Parser.Tokenizer.Token;

namespace Loom.Parser.AST.Rules.Default;

public record ParameterNode(TypeNode Type, Token Name) : ASTNode
{
    /// <inheritdoc/>
    public override IEnumerable<ASTNode> Children => Enumerable.Empty<ASTNode>();
}

public record ParameterListNode : ListNode<ParameterNode>
{
    /// <summary> Initializes a new instance of the <see cref="ParameterListNode"/> class. </summary>
    public ParameterListNode(List<ParameterNode> parameters) : base(parameters) { }
}

[ParserRule]
public class ParameterListRule : ParserRule<ParameterListNode>
{
    /// <inheritdoc/>
    public ParameterListRule(LoomParser parser) : base(parser) { }

    /// <inheritdoc/>
    public override ParameterListNode ParseNode()
    {
        Parser.Reader.Expect(TokenType.LParen); // (

        var parameters = new List<ParameterNode>();
        if (Parser.Reader.Peek(0).Type != TokenType.RParen)
        {
            do
            {
                var type = RunRule<TypeRule, TypeNode>(); // type
                var name = Parser.Reader.Expect(TokenType.Identifier); // name

                parameters.Add(new ParameterNode(type, name));
            }
            while (Parser.Reader.Match(TokenType.Comma));
        }

        Parser.Reader.Expect(TokenType.RParen); // )

        return new ParameterListNode(parameters);
    }
}
