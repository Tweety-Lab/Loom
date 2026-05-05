using LLVMSharp.Interop;
using Loom.Analyzer;
using Loom.CodeGen.LLVM;
using Loom.Common;
using Loom.Common.Diagnostics;
using Loom.LIR;
using Loom.LIR.Objects;
using Loom.Parser;

namespace Loom.CLI;

public class Program
{
    public const string TEST_SOURCE = @"
module Consumer
{   
    // Entry Point
    export i32 Main()
    {
        i32 result = Return();
        return result;
    }

    i32 Return()
    {
        return 1 + 1;
    }

}
";

    static void Main(string[] args)
    {
        CompilationContext context = new CompilationContext();
        context.Parse(TEST_SOURCE).Analyze().EmitLIR();

        LIRCompilationUnit unit = context.CompilationUnits.First();

        LLVMTranslatorPass translator = new LLVMTranslatorPass();
        translator.Run(unit);

        string llvmIr = translator.Result.PrintToString();
        Console.WriteLine("===== LLVM RESULT =====");
        Console.WriteLine(llvmIr);

        LLVM.InitializeNativeTarget();
        LLVM.InitializeNativeAsmPrinter();
        LLVM.InitializeNativeAsmParser();

        LLVMExecutionEngineRef engine = translator.Result.CreateExecutionEngine();
        LLVMValueRef main = translator.Result.GetNamedFunction(context.AnalysisContext.EntryPoint.FullyQualifiedName);
        LLVMGenericValueRef result = engine.RunFunction(main, []);

        unsafe
        {
            Console.WriteLine($"Result: {LLVM.GenericValueToInt(result, 1)}");
        }

        foreach (var diagnostic in context.DiagnosticContext.Diagnostics)
        {
            Console.ForegroundColor = diagnostic.Level switch
            {
                Diagnostic.DiagnosticLevel.Error => ConsoleColor.Red,
                Diagnostic.DiagnosticLevel.Warning => ConsoleColor.Yellow,
                _ => ConsoleColor.White
            };

            Console.WriteLine(diagnostic.Message);
        }

        Console.ResetColor();
    }
}
