using Loom.Common.Exceptions;
using Loom.Parser.AST;

using static Loom.Parser.Tokenizer.Token;

namespace Loom.Parser.Rules.Default;

public abstract record StatementNode() : ASTNode;

[ParserRule]
public class StatementRule : ParserRule<StatementNode>
{
    /// <inheritdoc/>
    public StatementRule(LoomParser parser) : base(parser) { }

    /// <inheritdoc/>
    public override StatementNode Parse()
    {
        var statement = Parser.Reader.Current.Type switch
        {
            TokenType.Return => RunRule<ReturnStatementRule, ReturnStatementNode>(),
            _ => throw new LoomException($"Unexpected token: {Parser.Reader.Current.Value}")
        };

        Parser.Reader.Expect(TokenType.Semicolon); // ;
        return statement;
    }
}
