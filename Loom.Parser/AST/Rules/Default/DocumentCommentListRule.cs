using static Loom.Parser.Tokenizer.Token;

namespace Loom.Parser.AST.Rules.Default;

public record DocumentCommentListNode : ListNode<DocumentCommentNode>
{
    /// <summary> Initializes a new instance of the <see cref="InheritanceListNode"/> class. </summary>
    public DocumentCommentListNode(List<DocumentCommentNode> baseComments) : base(baseComments) { }
}

[ParserRule]
public class DocumentCommentListRule : ParserRule<DocumentCommentListNode>
{
    /// <inheritdoc/>
    public DocumentCommentListRule(LoomParser parser) : base(parser) { }

    /// <inheritdoc/>
    public override DocumentCommentListNode ParseNode()
    {
        var baseComments = new List<DocumentCommentNode>();

        while (Parser.Reader.Current.Type == TokenType.DocumentComment)
            baseComments.Add(RunRule<DocumentCommentRule, DocumentCommentNode>());


        return new DocumentCommentListNode(baseComments);
    }
}
