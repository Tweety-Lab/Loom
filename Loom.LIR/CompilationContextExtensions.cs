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
        /// <summary> All <see cref="LIRCompilationUnit"/>s that make up the IR compilation of the current <see cref="CompilationContext"/>. </summary>
        public IEnumerable<LIRCompilationUnit> CompilationUnits => ctx.ExtendedProperties[LIRGEN_CONTEXT_KEY] as IEnumerable<LIRCompilationUnit> ?? throw new InvalidOperationException("CompilationContext.CompilationUnits is null, has LIR emmission been run?");

        /// <summary> Runs the <see cref="CompilationContext"/> through the Loom Intermediate Representation generation pipeline. </summary>
        public CompilationContext EmitLIR()
        {
            LIRCompilationUnit comp = new LIRCompilationUnit(new List<LIRFunction>());
            comp.MetaData.Add("Name", "MyFile.loom");

            LIRFunction function = comp.DefineFunction("main", new LIRFunctionType(LIRType.Void, []));

            ctx.ExtendedProperties[LIRGEN_CONTEXT_KEY] = new List<LIRCompilationUnit> { comp };

            LIRPrinter printer = new LIRPrinter(new StringPrinterStyle());
            string lir = printer.Print(comp);
            Console.WriteLine(lir);

            return ctx;
        }
    }
}


