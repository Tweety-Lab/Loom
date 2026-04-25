using Loom.Parser.AST;

namespace Loom.Analyzer.Analyzers;

/// <summary>
/// Base class for any object that inspects Loom code.
/// </summary>
public abstract class Analyzer : ASTWalker
{
    /// <summary> The context in which the analyzer is running. </summary>
    public AnalysisContext Context { get; set; } = null!;
}
