using Loom.Analyzer.Symbols;
using Loom.Parser.Rules.Default;

namespace Loom.Analyzer.Analyzers;

/// <summary>
/// Debug analyzer.
/// </summary>
[LoomAnalyzer]
public class UnresolvedImportAnalyzer : Analyzer
{
    /// <inheritdoc />
    public override void Visit(ImportNode node)
    {
        string module = node.ModuleName;
        ModuleSymbol? symbol = Context.SymbolTables.First().Value.Resolve(module) as ModuleSymbol;

        if (symbol is null)
            ReportException($"Unresolved import: {module}");

        base.Visit(node);
    }
}
