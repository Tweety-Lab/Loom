using static Loom.Parser.Tokenizer.Token;

namespace Loom.Parser.AST.Rules.Default;

public record InheritanceListNode : ListNode<TypeNode>
{
    /// <summary> Initializes a new instance of the <see cref="InheritanceListNode"/> class. </summary>
    public InheritanceListNode(List<TypeNode> baseTypes) : base(baseTypes) { }
}

[ParserRule]
public class InheritanceListRule : ParserRule<InheritanceListNode>
{
    /// <inheritdoc/>
    public InheritanceListRule(LoomParser parser) : base(parser) { }

    /// <inheritdoc/>
    public override InheritanceListNode ParseNode()
    {
        Parser.Reader.Expect(TokenType.Colon);

        var baseTypes = new List<TypeNode>();
        do
        {
            baseTypes.Add(RunRule<TypeRule, TypeNode>());
        }
        while (Parser.Reader.Match(TokenType.Comma));

        return new InheritanceListNode(baseTypes);
    }
}