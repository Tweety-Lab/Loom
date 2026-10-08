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
        if (TryRunRule<ArrayLiteralExpressionRule, ArrayLiteralNode>(out var arrayLiteral))
            return arrayLiteral;

        if (TryRunRule<StringLiteralExpressionRule, StringLiteralNode>(out var stringLiteral))
            return stringLiteral;

        if (TryRunRule<CharacterLiteralExpressionRule, CharacterLiteralNode>(out var characterLiteral))
            return characterLiteral;

        if (TryRunRule<BooleanLiteralExpressionRule, BooleanLiteralNode>(out var booleanLiteral))
            return booleanLiteral;

        if (TryRunRule<NumberLiteralExpressionRule, NumberLiteralNode>(out var numberLiteral))
            return numberLiteral;

        if (TryRunRule<InstanceCreationExpressionRule, InstanceCreationExpresssionNode>(out var instanceCreation))
            return instanceCreation;

        if (TryRunRule<DefaultExpressionRule, DefaultExpressionNode>(out var defaultLiteral))
            return defaultLiteral;

        if (TryRunRule<SizeOfExpressionRule, SizeOfExpressionNode>(out var sizeOfExpression))
            return sizeOfExpression;

        if (Parser.Reader.Current.Type is TokenType.Identifier)
            return new IdentifierNameNode(Parser.Reader.Advance());

        throw new ParseFailureException(Parser.Reader.Current, $"Unexpected token: '{Parser.Reader.Current.Text}' type={Parser.Reader.Current.Type}");
    }
}

