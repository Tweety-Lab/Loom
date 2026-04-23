using Loom.Analyser.Symbols;
using Loom.Common;
using Loom.Parser;
using Loom.Parser.AST;
using Loom.Parser.Rules.Default;

namespace Loom.Analyser;

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

        /// <summary> Runs the <see cref="CompilationContext"/> through the Semantic Analyser. </summary>
        public CompilationContext Analyse()
        {
            LoomAnalyser analyser = new();
            analyser.Analyse(ctx.RootNode ?? throw new InvalidOperationException("CompilationContext.RootNode is null, has parsing been run?"));

            ctx.ExtendedProperties[SYMBOL_DICT_KEY] = analyser.SymbolTables;

            return ctx;
        }
    }
}

