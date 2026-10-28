using static Loom.Common.Diagnostics.DiagnosticDescription;
using Loom.Analyzer.Symbols;
using Loom.Common.Diagnostics;
using Loom.Parser.AST;
using Loom.Parser.AST.Rules.Default;

namespace Loom.Analyzer.Analyzers;

/// <summary>
/// Checks for statements that appear outside of any method, e.g. directly inside a module or struct body.
/// </summary>
[LoomAnalyzer]
public class InvalidStatementScopeAnalyzer : Analyzer
{
    public static DiagnosticDescription InvalidStatementScope = new($"LM{Code:D4}", Diagnostic.DiagnosticLevel.Error, "Statement cannot exist outside of a method body.");

    [Visitor]
    public void Visit(StatementNode node)
    {
        if (Context.FirstAncestorOrSelf<MethodDeclarationNode>(node) == null && Context.FirstAncestorOrSelf<ConstructorDeclarationNode>(node) == null && Context.FirstAncestorOrSelf<DestructorDeclarationNode>(node) == null && Context.FirstAncestorOrSelf<ModuleNode>(node) == null && Context.FirstAncestorOrSelf<OperatorDeclarationNode>(node) == null)
            Context.DiagnosticContext?.Report(InvalidStatementScope, node.Span.StartToken?.Location);
    }
}