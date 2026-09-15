using LLVMSharp.Interop;
using Loom.Analyzer;
using Loom.Analyzer.Symbols;
using Loom.CodeGen.LLVM;
using Loom.Common;
using Loom.Common.Diagnostics;
using Loom.LIR;
using Loom.LIR.Objects;
using Loom.Parser;

namespace Loom.CLI;

public class Program
{
    static void Main(string[] args)
    {
        string projectSource = File.ReadAllText("Project/Program.loom");

        CompilationContext context = new CompilationContext();

        context.Parse(projectSource).Analyze().EmitLIR();

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

        PrintDiagnostics(context.DiagnosticContext);

        unsafe
        {
            Console.WriteLine($"Result: {LLVM.GenericValueToInt(result, 1)}");
        }
    }

    private static void PrintDiagnostics(DiagnosticContext context)
    {
        foreach (var diagnostic in context.Diagnostics)
        {
            Console.ForegroundColor = diagnostic.Level switch
            {
                Diagnostic.DiagnosticLevel.Error => ConsoleColor.Red,
                Diagnostic.DiagnosticLevel.Warning => ConsoleColor.Yellow,
                _ => ConsoleColor.White
            };

            Console.WriteLine($"[{diagnostic.Level}] {diagnostic.Message}");
        }

        Console.ResetColor();
    }
}
