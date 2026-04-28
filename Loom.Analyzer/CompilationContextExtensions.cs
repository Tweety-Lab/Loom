using Loom.Analyzer.Symbols;
using Loom.Common;
using Loom.Parser;
using Loom.Parser.AST;
using Loom.Parser.Rules.Default;

namespace Loom.Analyzer;

/// <summary>
/// Adds semantic analysis extensions to <see cref="CompilationContext"/>.
/// </summary>
public static class CompilationContextExtensions
{
    public const string ANALYSIS_CONTEXT_KEY = "Semantics.AnalysisContext";

    extension(CompilationContext ctx)
    {
        /// <summary> Maps <see cref="ASTNode"/>s to their corresponding <see cref="Binder"/>. </summary>
        public AnalysisContext AnalysisContext => ctx.ExtendedProperties[ANALYSIS_CONTEXT_KEY] as AnalysisContext ?? throw new InvalidOperationException("CompilationContext.AnalysisContext is null, has semantic analysis been run?");

        /// <summary> Runs the <see cref="CompilationContext"/> through the Semantic Analyzer. </summary>
        public CompilationContext Analyze()
        {
            AnalysisContext analyzer = new(ctx.DiagnosticContext);
            analyzer.Analyze(ctx.RootNode ?? throw new InvalidOperationException("CompilationContext.RootNode is null, has parsing been run?"));

            ctx.ExtendedProperties[ANALYSIS_CONTEXT_KEY] = analyzer;

            return ctx;
        }
    }
}

