using Loom.Common;
using Loom.Parser.Rules.Default;
using Loom.Parser.Tokenizer;

namespace Loom.Parser;

/// <summary>
/// Adds parsing extensions to <see cref="CompilationContext"/>.
/// </summary>
public static class CompilationContextExtensions
{
    public const string ROOT_NODE_KEY = "AST.RootNode";

    extension(CompilationContext ctx)
    {
        /// <summary> All parsed <see cref="ProgramNode"/>s. </summary>
        public IReadOnlyList<ProgramNode> SyntaxTrees => ctx.ExtendedProperties.TryGetValue(ROOT_NODE_KEY, out var obj) ? (List<ProgramNode>)obj : [];

        /// <summary> Runs the <see cref="CompilationContext"/> through the Parser. </summary>
        public CompilationContext Parse(params string[] inputs)
        {
            var trees = new List<ProgramNode>();

            foreach (var input in inputs)
            {
                LoomTokenizer tokenizer = new(input);
                tokenizer.Tokenize();

                LoomParser parser = new LoomParser(tokenizer.Tokens, ctx.DiagnosticContext);
                trees.Add(parser.ParseProgram());
            }

            ctx.ExtendedProperties[ROOT_NODE_KEY] = trees;
            return ctx;
        }
    }
}
