using Loom.Parser.AST;
using System;
using System.Collections.Generic;
using System.Text;
using static Loom.Parser.Tokenizer.Token;

namespace Loom.Parser.Rules.Default;

public record CallExpressionNode(IdentifierNameNode MethodName, List<ExpressionNode> Arguments) : ExpressionNode
{
    /// <inheritdoc/>
    public override IEnumerable<ASTNode> Children => Arguments.Prepend(MethodName);
}

[ParserRule]
public class CallExpressionRule : ParserRule<CallExpressionNode>
{
    /// <inheritdoc/>
    public CallExpressionRule(LoomParser parser) : base(parser) { }

    /// <inheritdoc/>
    public override CallExpressionNode ParseNode()
    {
        var callName = Parser.Reader.Expect(TokenType.Identifier); // name

        Parser.Reader.Expect(TokenType.LParen); // (

        // Arguments

        Parser.Reader.Expect(TokenType.RParen); // )

        return new CallExpressionNode(new IdentifierNameNode(callName), new List<ExpressionNode>());
    }
}

