using Loom.Common;
using Loom.Parser;

namespace Loom.Analyser;

/// <summary>
/// Adds semantic analysis extensions to <see cref="CompilationContext"/>.
/// </summary>
public static class CompilationContextExtensions
{
    extension(CompilationContext ctx)
    {
        /// <summary> Runs the <see cref="CompilationContext"/> through the Semantic Analyser. </summary>
        public CompilationContext Analyse()
        {
            LoomAnalyser analyser = new();
            analyser.Analyse(ctx.RootNode ?? throw new InvalidOperationException("CompilationContext.RootNode is null, has parsing been run?"));
            return ctx;
        }
    }
}

