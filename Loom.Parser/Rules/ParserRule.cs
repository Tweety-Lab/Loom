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
    /// <summary> Loops until <paramref name="until"/> is matched, dispatching to handlers by token type. Throws diagnostics on unregistered tokens. </summary>
    protected void ParseUntil(TokenType until, Dictionary<TokenType, Action> handlers, Action? fallback = null)
    {
        while (!Parser.Reader.Check(until))
        {
            var token = Parser.Reader.Current;

            if (handlers.TryGetValue(token.Type, out var handler))
                handler();
            else if (fallback != null)
                fallback();
            else
                Parser.DiagnosticContext?.Report(new Common.Diagnostics.Diagnostic(Common.Diagnostics.Diagnostic.DiagnosticLevel.Error, $"Unexpected token: {token.Value}"));
        }
    }
}
