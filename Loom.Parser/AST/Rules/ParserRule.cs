using Loom.Parser.AST;
using System.Diagnostics.CodeAnalysis;
using static Loom.Parser.Tokenizer.Token;

namespace Loom.Parser.AST.Rules;

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
    public abstract T ParseNode();

    public T Parse()
    {
        var startToken = Parser.Reader.Current;
        var node = ParseNode();
        node.StartToken = startToken;
        return node;
    }

    /// <inheritdoc/>
    public ASTNode? ParseUntyped(LoomParser parser) => Parse();

    /// <summary> Runs a <see cref="ParserRule{T}"/> and commits to its result. </summary>
    /// <remarks> Use this where the rule should always apply. Use <see cref="TryRunRule{TRule, TNode}"/> where the match is ambiguous. </remarks>
    protected TNode RunRule<TRule, TNode>() where TRule : ParserRule<TNode> where TNode : ASTNode => Parser.GetRule<TRule>().Parse();

    /// <summary> Attempts to run a <see cref="ParserRule{T}"/>, rolling the parser back if it does not apply. </summary>
    /// <returns> <see langword="true"/> if the rule ran without errors; otherwise, <see langword="false"/>. </returns>
    protected bool TryRunRule<TRule, TNode>([NotNullWhen(true)] out TNode? node) where TRule : ParserRule<TNode> where TNode : ASTNode
    {
        var context = Parser.DiagnosticContext;
        int position = Parser.Reader.Position;
        int diagnostics = context?.Diagnostics.Count ?? 0;

        try
        {
            node = Parser.GetRule<TRule>().Parse();
        }
        catch (ParseFailureException)
        {
            Parser.Reader.Position = position;
            context?.Rewind(diagnostics);
            node = default!;
            return false;
        }

        if (context is not null && context.Diagnostics.Count > diagnostics)
        {
            Parser.Reader.Position = position;
            context.Rewind(diagnostics);
            node = default!;
            return false;
        }

        return true;
    }

    /// <summary> Loops until <paramref name="until"/> is matched, dispatching to handlers by token type. Reports diagnostics on unregistered tokens and on rules that fail. </summary>
    protected void ParseUntil(TokenType until, Dictionary<TokenType, Action> handlers, Action? fallback = null)
    {
        while (!Parser.Reader.Check(until))
        {
            var position = Parser.Reader.Position;
            var token = Parser.Reader.Current;

            try
            {
                if (handlers.TryGetValue(token.Type, out var handler))
                    handler();
                else if (fallback != null)
                    fallback();
                else
                    Parser.DiagnosticContext?.Report(new Common.Diagnostics.Diagnostic(Common.Diagnostics.Diagnostic.DiagnosticLevel.Error, $"Unexpected token: {token.Text}"), token.Location);
            }
            catch (ParseFailureException)
            {

            }

            if (Parser.Reader.Position == position)
                Parser.Reader.Advance();
        }
    }
}
