using Loom.Common;
using Loom.LIR.Generators;
using Loom.LIR.OpCodes;
using Loom.LIR.Printers;

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
            LIRGenerator il = new();
            var A = il.Emit(LIROpCode.Const, new LIRConstantValue(1));
            var B = il.Emit(LIROpCode.Const, new LIRConstantValue(2));

            var sum = il.Emit(LIROpCode.Add, A, B);

            il.Emit(LIROpCode.Ret, sum);

            LIRPrinter printer = new();
            string lir = printer.Print(il.Instructions.ToList());
            Console.WriteLine(lir);

            return ctx;
        }
    }
}


