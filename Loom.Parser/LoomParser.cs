using Loom.Common.Diagnostics;
using Loom.Common.Reflection;
using Loom.Parser.AST.Rules;
using Loom.Parser.AST.Rules.Default;
using Loom.Parser.Tokenizer;
using System.Reflection;

namespace Loom.Parser;

public class LoomParser
{
    /// <summary> The underlying <see cref="TokenReader"/>. />
    public TokenReader Reader { get; }

    /// <summary> The <see cref="Common.Diagnostics.DiagnosticContext"/> the parser will report to, if any. </summary>
    public DiagnosticContext? DiagnosticContext { get; }

    /// <summary> All registered rules the parser uses. </summary>
    public List<IParserRule> Rules
    {
        get
        {
            if (field != null)
                return field;

            field = LoomReflection.InstansiateAllWithAttribute<ParserRuleAttribute>(Assembly.GetExecutingAssembly(), this).OfType<IParserRule>().ToList();

            return field;
        }
    }

    /// <summary> Initializes a new instance of the <see cref="LoomParser"/> class. </summary>
    public LoomParser(List<Token> tokens, DiagnosticContext? diagnosticContext = null) => (Reader, DiagnosticContext) = (new TokenReader(tokens, diagnosticContext), diagnosticContext);

    /// <summary> Gets the rule for the given type. </summary>
    public T GetRule<T>() where T : IParserRule => (T)Rules.First(x => x.GetType() == typeof(T));

    /// <summary> Gets registered rules assignable to the given rule type, in registration order. </summary>
    public IEnumerable<T> GetRules<T>() where T : IParserRule => Rules.OfType<T>();

    /// <summary> Gets the source range consumed since a token index. </summary>
    public AST.Span GetSpan(int startPosition)
    {
        var start = Reader.Tokens[startPosition];
        var end = Reader.Position > startPosition ? Reader.Tokens[Reader.Position - 1].End : start.Start;
        return new AST.Span(start.Start, end, start);
    }

    /// <summary> Attaches consumed syntax, including delimiters, to a parsed node. </summary>
    public T Complete<T>(T node, int startPosition) where T : AST.ASTNode
    {
        var span = GetSpan(startPosition);
        if (node.Span.StartToken is not null && node.Span.Start < span.Start)
            span = new AST.Span(node.Span.Start, span.End, node.Span.StartToken);

        node.Span = span;
        return node;
    }

    /// <summary> Anchors an omitted syntax list at the current source position. </summary>
    public T Empty<T>(T node) where T : AST.ASTNode
    {
        node.Span = new AST.Span(Reader.Current.Start, Reader.Current.Start, null);
        return node;
    }

    /// <summary> Parses the given source code into a root <see cref="ProgramNode"/>. </summary>
    public ProgramNode ParseProgram(string name)
    {
        ProgramNode node = new ProgramRule(this).Parse();
        node.Name = name;
        return node;
    }
}
