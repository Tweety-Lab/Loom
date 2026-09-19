using Loom.Analyzer;
using Loom.CLI.JustInTime;
using Loom.Common.Diagnostics;

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

            if (llvm.TryExecute(sample.CompilationContext.AnalysisContext.EntryPoint!, out IJITResult? result))
            {
                if (result == null)
                    return;

                Console.WriteLine($"Result: {result.ToInt32()}");
            }
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
