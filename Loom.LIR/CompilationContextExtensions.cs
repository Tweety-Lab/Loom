using Loom.Common;
using Loom.LIR.Builders;
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
            CompilationUnitBuilder builder = new();
            var func = builder.DefineFunction("TestModule::MyFunc");
            var il = func.LIRGenerator;

            var a = il.Emit(LIROpCode.Const, new LIRConstantValue(1));
            var b = il.Emit(LIROpCode.Const, new LIRConstantValue(1));
            var sum = il.Emit(LIROpCode.Add, a, b);

            il.Emit(LIROpCode.Ret, sum);

            var unit = builder.Build();

            LIRPrinter printer = new();
            string lir = printer.Print(unit);

            Console.WriteLine(lir);

            return ctx;
        }
    }
}


