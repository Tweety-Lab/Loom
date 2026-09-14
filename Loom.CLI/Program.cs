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
    public const string TEST_SOURCE = @"
import Windows;

module Consumer
{
    // Entry Point
    export i32 Main()
    {
        Sleep(1000);

        Process process = GetProcess();
        process.Close();

        return 1;
    }
}

module Windows
{
    export extern void Sleep(i32 length);
    export extern iptr GetCurrentProcess();
    export extern bool TerminateProcess(iptr process, i32 exitCode);

    export struct Process
    {
        iptr Handle = 0;

        void Close()
        {
            TerminateProcess(Handle, 0);
            return;
        }
    }

    export Process GetProcess()
    {
        Process process = new Process();
        process.Handle = GetCurrentProcess();
        return process;
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
