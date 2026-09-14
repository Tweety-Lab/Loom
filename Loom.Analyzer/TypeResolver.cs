using Loom.Analyzer.Symbols;
using Loom.Parser.AST;
using Loom.Parser.Rules.Default;

namespace Loom.Analyzer;

/// <summary>
/// Resolves type names within the current scope, falling back to types exported by imported modules.
/// </summary>
internal static class TypeResolver
{
    /// <summary> Resolves a type by name in the scope of <paramref name="node"/>, or null if it could not be found. </summary>
    public static TypeSymbol? Resolve(AnalysisContext context, ASTNode node, string name)
    {
        if (context.GetBinder(node)?.Lookup(name)?.FirstOrDefault(s => s is TypeSymbol) is TypeSymbol inScope)
            return inScope;

        var programNode = context.FirstAncestorOrSelf<ProgramNode>(node);
        if (programNode == null)
            return null;

        foreach (var import in programNode.Imports)
        {
            var moduleSymbol = context.GetSymbol(import.ModuleName).Symbol as ModuleSymbol;
            if (moduleSymbol?.DeclaringNode is not ModuleNode moduleNode)
                continue;

            if (context.Binders.TryGetValue(moduleNode, out var moduleBinder)
                && moduleBinder.Lookup(name)?.FirstOrDefault(s => s is TypeSymbol) is TypeSymbol imported)
                return imported;
        }

        return null;
    }
}