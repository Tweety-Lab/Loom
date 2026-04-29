using Loom.Common;
using Loom.LIR.Objects;
using Loom.LIR.Printers;
using Loom.Parser;

namespace Loom.LIR;

/// <summary>
/// Adds Loom Intermediate Representation extensions to <see cref="CompilationContext"/>.
/// </summary>
public static class CompilationContextExtensions
{
    public const string LIRGEN_CONTEXT_KEY = "LIRGen.Context";

    extension(CompilationContext ctx)
    {
        /// <summary> Runs the <see cref="CompilationContext"/> through the Loom Intermediate Representation generation pipeline. </summary>
        public CompilationContext EmitLIR()
        {
            foreach (var syntaxTree in ctx.SyntaxTrees)
            {
                LIRASTWalker walker = new(ctx);
                LIRCompilationUnit comp = walker.Build(syntaxTree);

                LIRPrinter printer = new LIRPrinter(new StringPrinterStyle());
                string lir = printer.Print(comp);
                Console.WriteLine(lir);
            }

            return ctx;
        }
    }
}


