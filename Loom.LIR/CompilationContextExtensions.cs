using Loom.Common;
using Loom.LIR.Objects;
using Loom.LIR.OpCodes;
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

            LIRFunctionType mainType = new LIRFunctionType(LIRType.Int32, []);
            LIRFunction function = comp.DefineFunction("main", mainType);

            LIRTempValue intAddress = function.LIRGenerator.Emit(LIROpCode.Alloca, new LIRPointerType(LIRType.Int32));
            function.LIRGenerator.Emit(LIROpCode.Store, null, new LIRConstantIntValue(1), intAddress);

            LIRTempValue x = function.LIRGenerator.Emit(LIROpCode.Load, LIRType.Int32, intAddress);
            
            LIRTempValue result = function.LIRGenerator.Emit(LIROpCode.Add, LIRType.Int32, x, new LIRConstantIntValue(1));
            function.LIRGenerator.Emit(LIROpCode.Return, null, result);

            ctx.ExtendedProperties[LIRGEN_CONTEXT_KEY] = new List<LIRCompilationUnit> { comp };

            LIRPrinter printer = new LIRPrinter(new StringPrinterStyle());
            string lir = printer.Print(comp);
            Console.WriteLine(lir);

            return ctx;
        }
    }
}


