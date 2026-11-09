using Loom.Analyzer;
using Loom.CLI.JustInTime;
using Loom.Common.Diagnostics;
using Loom.Parser;
using Loom.Parser.AST.Rules.Default;
using CommandLine;
using System.Diagnostics.CodeAnalysis;

namespace Loom.CLI.Commands;

[Verb("run")]
internal class RunCommand : Command
{
    [Option('p', "path")]
    public string Path { get; set; } = "./";

    /// <inheritdoc/>
    public override void Execute()
    {
        string? lmprojPath = null;

        if (File.Exists(Path) && System.IO.Path.GetExtension(Path) == ".lmproj")
            lmprojPath = Path;
        else if (TryGetProjectInPath(Path, out lmprojPath) == false)
            throw new Exception("Could not find a *.lmproj file at the given path.");

        LoomProject sample = new LoomProject(lmprojPath);
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

    private static bool TryGetProjectInPath(string path, [NotNullWhen(true)] out string? lmprojPath)
    {
        lmprojPath = null;

        var directory = new DirectoryInfo(path);
        while (directory != null)
        {
            var project = directory.EnumerateFiles("*.lmproj", SearchOption.TopDirectoryOnly).FirstOrDefault();

            if (project != null)
            {
                lmprojPath = project.FullName;
                return true;
            }

            directory = directory.Parent;
        }

        return false;
    }
}
