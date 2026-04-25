using Loom.Common.Exceptions;
using Loom.Parser.AST;

using static Loom.Parser.Tokenizer.Token;

namespace Loom.Parser.Rules.Default;


public abstract record ExpressionNode() : ASTNode
{
    /// <inheritdoc/>
    public override IEnumerable<ASTNode> Children => Enumerable.Empty<ASTNode>();
}

public record NumberLiteralNode(string Value) : ExpressionNode
{
    /// <inheritdoc/>
    public override IEnumerable<ASTNode> Children => Enumerable.Empty<ASTNode>();
}

[ParserRule]
public class ExpressionRule : ParserRule<ExpressionNode>
{
    /// <inheritdoc/>
    public ExpressionRule(LoomParser parser) : base(parser) { }

    /// <inheritdoc/>
    public override ExpressionNode Parse()
    {
        return Parser.Reader.Current.Type switch
        {
            TokenType.Number => new NumberLiteralNode(Parser.Reader.Advance().Value),
            _ => throw new LoomException($"Unexpected token: {Parser.Reader.Current.Value}")
        };
    }
}

