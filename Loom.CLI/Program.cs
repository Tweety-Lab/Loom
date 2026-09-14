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
    if (1 == 0) { }

    export struct MathInstance
    {
        i32 Number = 0;

        i32 Add(i32 a, i32 b)
        {
            return a + b;
        }
    }

    // Entry Point
    export i32 Main()
    {
        Sleep(1000);

        MathInstance math = new MathInstance();
        i32 result = math.Add(2, 4);
        if (result == 2 + 4)
        {
            result = 20;
        }

        return result;
    }
}

module Windows
{
    export extern void Sleep(i32 length);
    export extern iptr GetCurrentProcess();
}
";

    static void Main(string[] args)
    {
        CompilationContext context = new CompilationContext();

        try
        {
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
        catch (Exception)
        {
            PrintDiagnostics(context.DiagnosticContext);
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

            Console.WriteLine(diagnostic.Message);
        }

        Console.ResetColor();
    }
}
