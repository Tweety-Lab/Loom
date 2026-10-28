using static Loom.Common.Diagnostics.DiagnosticDescription;
using Loom.Analyzer.Symbols;
using Loom.Common.Diagnostics;
using Loom.Parser.AST;
using Loom.Parser.AST.Rules.Default;

namespace Loom.Analyzer.Analyzers;


/// <summary>
/// Checks for method body rules.
/// </summary>
[LoomAnalyzer]
public class MethodBodyAnalyzer : Analyzer
{
    public static DiagnosticDescription MethodNeedsBody = new($"LM{Code:D4}", Diagnostic.DiagnosticLevel.Error, "Method '{0}' must declare a body because it is not marked extern.");

    [Visitor]
    public void Visit(MethodDeclarationNode node)
    {
        if (Context.GetSymbol(node).Symbol is not MethodSymbol symbol)
            return;

        if (symbol.ContainingType?.KnownType == TypeSymbol.DefaultType.Interface)
            return;

        if (node.Body == null && !symbol.IsExtern)
            Context.DiagnosticContext?.Report(MethodNeedsBody, node.Span.StartToken?.Location, symbol.FullyQualifiedName);
    }
}
