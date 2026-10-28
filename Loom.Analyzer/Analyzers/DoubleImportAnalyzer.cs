using static Loom.Common.Diagnostics.DiagnosticDescription;
using Loom.Common.Diagnostics;
using Loom.Parser.AST;
using Loom.Parser.AST.Rules.Default;

namespace Loom.Analyzer.Analyzers;

/// <summary>
/// Checks for imports of an already imported module.
/// </summary>
[LoomAnalyzer]
public class DoubleImportAnalyzer : Analyzer
{
    public static DiagnosticDescription DoubleImport = new($"LM{Code:D4}", Diagnostic.DiagnosticLevel.Warning, "The module '{0}' has already been imported.");

    [Visitor]
    public void Visit(ProgramNode node)
    {
        var imports = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var import in node.Imports)
        {
            if (!imports.Add(import.ModuleName.BaseName))
                Context.DiagnosticContext?.Report(DoubleImport, import.ModuleName.Span.StartToken?.Location, import.ModuleName.BaseName);
        }
    }
}
