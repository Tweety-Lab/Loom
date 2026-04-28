using Loom.Analyzer.Symbols;
using Loom.Common.Diagnostics;
using Loom.Parser.AST;
using Loom.Parser.Rules.Default;

namespace Loom.Analyzer.Analyzers;

/// <summary>
/// Checks for imports that do not exist.
/// </summary>
[LoomAnalyzer]
public class UnresolvedImportAnalyzer : Analyzer
{
    public static Diagnostic UnresolvedImportDiagnostic = new(Diagnostic.DiagnosticLevel.Error, "The module '{0}' could not be resolved.");

    [Visitor]
    public void Visit(ImportNode node)
    {
        ModuleSymbol? symbol = Context.ResolveSymbol(node.ModuleName).Symbol as ModuleSymbol;

        if (symbol == null)
            Context.DiagnosticContext?.Report(UnresolvedImportDiagnostic, node.ModuleName.BaseName);
    }
}
