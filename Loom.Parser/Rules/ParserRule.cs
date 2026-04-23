using Loom.Common.Exceptions;
using Loom.Parser.AST;
using static Loom.Parser.Tokenizer.Token;

namespace Loom.Parser.Rules;

public interface IParserRule
{
    ASTNode? ParseUntyped(LoomParser parser);
}

public abstract class ParserRule<T> : IParserRule where T : ASTNode
{
    /// <summary> The underlying <see cref="LoomParser"/>. </summary>
    protected LoomParser Parser { get; init; }

    /// <summary> Initializes a new instance of the <see cref="ParserRule{T}"/> class. </summary>
    public ParserRule(LoomParser parser) => Parser = parser;

    /// <summary> Parses <typeparamref name="T"/>. </summary>
    public abstract T Parse();

    /// <inheritdoc/>
    public ASTNode? ParseUntyped(LoomParser parser) => Parse();

    /// <summary> Runs a <see cref="ParserRule{T}"/>. </summary>
    protected TNode RunRule<TRule, TNode>() where TRule : ParserRule<TNode> where TNode : ASTNode => Parser.GetRule<TRule>().Parse();

    // TODO: Improve this design
    /// <summary> Loops until <paramref name="until"/> is matched, dispatching to handlers by token type. Throws <see cref="LoomException"/> on unregistered tokens. </summary>
    protected void ParseUntil(TokenType until, Dictionary<TokenType, Action> handlers)
    {
        while (!Parser.Reader.Check(until))
        {
            var token = Parser.Reader.Current;

            if (handlers.TryGetValue(token.Type, out var handler))
                handler();
            else
                throw new LoomException($"Unexpected token: {token.Value}");
        }
    }
}
