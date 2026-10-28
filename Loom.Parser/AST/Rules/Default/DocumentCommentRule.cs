using Loom.Parser.Tokenizer;
using static Loom.Parser.Tokenizer.Token;
using Loom.Common.Diagnostics;
using System.Text.RegularExpressions;

namespace Loom.Parser.AST.Rules.Default;

// Storing an enum and checking it like this is something that we should probably do in the analysis phase
public enum DocumentCommentDirective
{
    Brief,
    Param,
    TParam,
    Return,
    Remark,
    InheritDoc
}

public record DocumentCommentNode(DocumentCommentDirective Directive, string? Target, string Body) : ASTNode
{
    /// <inheritdoc/>
    public override IEnumerable<ASTNode> Children => Enumerable.Empty<ASTNode>();
}

[ParserRule]
public class DocumentCommentRule : ParserRule<DocumentCommentNode>
{
    /// <inheritdoc/>
    public DocumentCommentRule(LoomParser parser) : base(parser) { }

    /// <inheritdoc/>
    public override DocumentCommentNode ParseNode()
    {
        // It's really dumb we do it like this
        var token = Parser.Reader.Expect(TokenType.DocumentComment);
        var match = Regex.Match(token.Text, @"^///[ \t]*@(?<directive>\w+)(?:[ \t]*\([ \t]*(?<target>\w+)[ \t]*\))?[ \t]*(?<body>.*)$");

        if (!match.Success)
        {
            string message = "Expected documentation directive. (i.e., @brief)";
            Parser.DiagnosticContext?.Report(new Diagnostic("LM0049", Diagnostic.DiagnosticLevel.Error, message), token.Location);
            throw new ParseFailureException(token, message);
        }

        if (!Enum.TryParse<DocumentCommentDirective>(match.Groups["directive"].Value, ignoreCase: true, out var directive) || !Enum.IsDefined(directive))
        {
            string message = $"Unknown documentation directive '{match.Groups["directive"].Value}'.";
            Parser.DiagnosticContext?.Report(new Diagnostic("LM0050", Diagnostic.DiagnosticLevel.Error, message), token.Location);
            throw new ParseFailureException(token, message);
        }

        return new DocumentCommentNode(directive, match.Groups["target"].Success ? match.Groups["target"].Value : null, match.Groups["body"].Value);
    }
}
