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
    public const string SYMBOL_DICT_KEY = "Semantics.SymbolDict";

    extension(CompilationContext ctx)
    {
        /// <summary> Maps <see cref="ASTNode"/>s to their corresponding <see cref="SymbolTable"/>. </summary>
        public Dictionary<ASTNode, SymbolTable>? SymbolMap => ctx.ExtendedProperties.TryGetValue(SYMBOL_DICT_KEY, out object? obj) ? (Dictionary<ASTNode, SymbolTable>)obj : null;

        /// <summary> Runs the <see cref="CompilationContext"/> through the Semantic Analyzer. </summary>
        public CompilationContext Analyse()
        {
            AnalysisContext Analyzer = new();
            Analyzer.Analyse(ctx.RootNode ?? throw new InvalidOperationException("CompilationContext.RootNode is null, has parsing been run?"));

            ctx.ExtendedProperties[SYMBOL_DICT_KEY] = Analyzer.SymbolTables;

            return ctx;
        }
    }
}

