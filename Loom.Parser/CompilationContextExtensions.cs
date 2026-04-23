using Loom.Common;
using Loom.Common.Exceptions;
using Loom.Parser.AST;
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
        /// <summary> The root <see cref="ASTNode"/> of the Abstract Syntax Tree or null if parsing has not been run. </summary>
        public ProgramNode? RootNode => ctx.ExtendedProperties.TryGetValue(ROOT_NODE_KEY, out object? obj) ? (ProgramNode)obj : null;

        /// <summary> Runs the <see cref="CompilationContext"/> through the Parser. </summary>
        public CompilationContext Parse(string input)
        {
            LoomTokenizer tokenizer = new(input);

            try
            {
                tokenizer.Tokenize();
            }
            catch (LoomException le)
            {
                ctx.ThrowException(le);
            }

            LoomParser parser = new LoomParser(tokenizer.Tokens);

            try
            {
                ProgramNode root = parser.ParseProgram();
                ctx.ExtendedProperties[ROOT_NODE_KEY] = root;
            }
            catch (LoomException le)
            {
                ctx.ThrowException(le);
            }

            return ctx;
        }
    }
}
