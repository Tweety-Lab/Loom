using Loom.Analyzer;
using Loom.CLI.JustInTime;
using Loom.Common.Diagnostics;
using Loom.Parser;
using Loom.Parser.AST.Rules.Default;

namespace Loom.CLI;

public class Program
{
    static void Main(string[] args)
    {
        LoomProject sample = new LoomProject(GetStandardPath());
        var buildResult = sample.Build();

        var firstClass = sample.CompilationContext.SyntaxTrees.SelectMany(tree => tree.Descendants).OfType<ClassDeclarationNode>().FirstOrDefault(node => node.DocumentComments.Items.Any());
        if (firstClass is not null)
            Console.WriteLine(firstClass.DocumentComments.Items.First().Body);

        if (buildResult != LoomProject.BuildResult.Success)
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

                Console.WriteLine($"Result: {result.ToInt64()}");
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

            var position = diagnostic.Position is { } p ? $" ({p.Line}, {p.Column})" : string.Empty;

            Console.WriteLine($"[{diagnostic.Level}{position}] {diagnostic.Message}");
        }

        Console.ResetColor();
    }

    private static string GetStandardPath()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            var path = Path.Combine(directory.FullName, "Loom.Standard", "Loom.Standard.lmproj");

            if (File.Exists(path))
                return path;

            directory = directory.Parent;
        }

        throw new FileNotFoundException("Could not find Standard/Standard.lmproj.");
    }
}
