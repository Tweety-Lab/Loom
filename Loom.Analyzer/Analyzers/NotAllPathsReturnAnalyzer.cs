using Loom.Common.Diagnostics;
using Loom.Parser.AST;
using Loom.Parser.Rules.Default;

namespace Loom.Analyzer.Analyzers;

/// <summary>
/// Checks for return statements for all paths in methods that return non-void.
/// </summary>
[LoomAnalyzer]
public class NotAllPathsReturnAnalyzer : Analyzer
{
    public static Diagnostic NotAllPathsReturnDiagnostic = new(Diagnostic.DiagnosticLevel.Error, "Not all code paths return a value.");

    [Visitor]
    public void Visit(MethodDeclarationNode node)
    {
        if (node.Modifiers.Any(m => m.Type == Parser.Tokenizer.Token.TokenType.Extern))
            return;

        if (node.ReturnType.Type == Parser.Tokenizer.Token.TokenType.Void)
            return;

        if (node.Body.Contents.All(x => x is not ReturnStatementNode))
            Context.DiagnosticContext?.Report(NotAllPathsReturnDiagnostic);
    }
}
