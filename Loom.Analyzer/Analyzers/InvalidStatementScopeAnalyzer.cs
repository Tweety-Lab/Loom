using Loom.Common.Diagnostics;
using Loom.Parser.AST;
using Loom.Parser.Rules.Default;

namespace Loom.Analyzer.Analyzers;

/// <summary>
/// Checks for statements that appear outside of any method, e.g. directly inside a module or struct body.
/// </summary>
[LoomAnalyzer]
public class InvalidStatementScopeAnalyzer : Analyzer
{
    public static Diagnostic InvalidStatementScope = new(Diagnostic.DiagnosticLevel.Error, "Statement cannot exist outside of a method.");

    [Visitor]
    public void Visit(StatementNode node)
    {
        if (Context.FirstAncestorOrSelf<MethodDeclarationNode>(node) == null)
            Context.DiagnosticContext?.Report(InvalidStatementScope, node.StartToken?.Location);
    }
}