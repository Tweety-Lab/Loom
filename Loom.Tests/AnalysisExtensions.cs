using Loom.Analyzer;
using Loom.Analyzer.Symbols;
using Loom.Common;
using Loom.Parser;
using Loom.Parser.AST;
using Loom.Parser.AST.Rules.Default;

namespace Loom.Tests;

public static class AnalysisExtensions
{
    extension(CompilationContext context)
    {
        /// <summary> Every module the context declares, optionally narrowed to the ones named <paramref name="module"/>. </summary>
        public IEnumerable<ModuleNode> Modules(string? module = null) => context.SyntaxTrees.SelectMany(tree => tree.Modules).Where(declared => module == null || declared.Name.Text == module);

        /// <summary> Every method named <paramref name="name"/> that the program declares. </summary>
        public IEnumerable<MethodSymbol> DeclaredMethods(string name) => context.Modules().SelectMany(module => context.AnalysisContext.Binders[module].Symbols).OfType<MethodSymbol>().Where(method => method.Name == name);

        /// <summary> Every call the given module makes, in source order. </summary>
        public IEnumerable<CallExpressionNode> Calls(string? module = null) => context.Modules(module).SelectMany(declared => declared.Descendants()).OfType<CallExpressionNode>();
    }

    extension(ASTNode node)
    {
        // TODO:
        // Should probably have a way to do this outside of tests
        /// <summary> Every node below this node, outermost first. </summary>
        public IEnumerable<ASTNode> Descendants() => node.Children.SelectMany(child => child.Descendants().Prepend(child));
    }
}