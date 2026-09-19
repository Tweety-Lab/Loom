using LLVMSharp.Interop;
using Loom.Analyzer;
using Loom.Analyzer.Symbols;
using Loom.CLI.JustInTime;
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
        LoomProject sample = new LoomProject("SampleProject/SampleProject.lmproj");
        if (sample.Build() != LoomProject.BuildResult.Success)
        {
            PrintDiagnostics(sample.CompilationContext.DiagnosticContext);
            return;
        }

        LLVMJITCompiler llvm = new LLVMJITCompiler();
        if (llvm.TryInitialize(sample))  
        {
            if (sample.CompilationContext.AnalysisContext.EntryPoint == null)
                return;

            llvm.TryExecute(sample.CompilationContext.AnalysisContext.EntryPoint!);
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
