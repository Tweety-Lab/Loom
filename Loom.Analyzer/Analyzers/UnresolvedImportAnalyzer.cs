using Loom.Analyzer.Symbols;
using Loom.Parser.AST;
using Loom.Parser.Rules.Default;

namespace Loom.Analyzer.Analyzers;

/// <summary>
/// Debug analyzer.
/// </summary>
[LoomAnalyzer]
public class UnresolvedImportAnalyzer : Analyzer
{
    [Visitor]
    public void Visit(ImportNode node)
    {
        ModuleSymbol? symbol = Context.ResolveSymbol(Context.SymbolTables.First().Key, node.ModuleName) as ModuleSymbol;

        if (symbol == null)
            ReportException($"Unresolved import: {node.ModuleName}");
    }
}
