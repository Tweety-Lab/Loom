using Loom.Parser.AST;
using Loom.Parser.AST.Rules;
using Loom.Parser.Tokenizer;
using static Loom.Parser.Tokenizer.Token;

namespace Loom.Parser.AST.Rules.Default;


public abstract record ExpressionNode() : ASTNode
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
    public override ExpressionNode ParseNode() => RunRule<EqualityExpressionRule, ExpressionNode>();
}

[ParserRule]
public class PrimaryExpressionRule : ParserRule<ExpressionNode>
{
    /// <inheritdoc/>
    public PrimaryExpressionRule(LoomParser parser) : base(parser) { }

    /// <inheritdoc/>
    public override ExpressionNode ParseNode()
    {
        return Parser.Reader.Current.Type switch
        {
            TokenType.LBracket => RunRule<ArrayLiteralExpressionRule, ArrayLiteralNode>(),
            TokenType.CharacterLiteral => RunRule<CharacterLiteralExpressionRule, CharacterLiteralNode>(),
            TokenType.True or TokenType.False => RunRule<BooleanLiteralExpressionRule, BooleanLiteralNode>(),
            TokenType.Number => RunRule<NumberLiteralExpressionRule, NumberLiteralNode>(),
            TokenType.New => RunRule<InstanceCreationExpressionRule, InstanceCreationExpresssionNode>(),
            TokenType.Identifier => new IdentifierNameNode(Parser.Reader.Advance()),
            TokenType.Default => RunRule<DefaultExpressionRule, DefaultLiteralNode>(),
            _ => throw new Exception($"Unexpected token: '{Parser.Reader.Current.Text}' type={Parser.Reader.Current.Type} at {Parser.Reader.Current.Location}")
        };
    }
}

