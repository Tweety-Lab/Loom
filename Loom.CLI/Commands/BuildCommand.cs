using Loom.Analyzer;
using Loom.CLI.JustInTime;
using Loom.Common.Diagnostics;
using CommandLine;
using System.Diagnostics.CodeAnalysis;

namespace Loom.CLI.Commands;

[Verb("build")]
internal class BuildCommand : Command
{
    [Option('p', "path")]
    public string Path { get; set; } = "./";

    [Option('t', "target")]
    public string? Target { get; set; }

    /// <inheritdoc/>
    public override void Execute()
    {
        LoomProject? output = Build();
        if (output == null)
            throw new Exception("Build Failed.");
    }

    public LoomProject? Build()
    {
        string? lmprojPath = null;

        if (File.Exists(Path) && System.IO.Path.GetExtension(Path) == ".lmproj")
            lmprojPath = Path;
        else if (TryGetProjectInPath(Path, out lmprojPath) == false)
            throw new Exception("Could not find a *.lmproj file at the given path.");

        LoomProject project = new LoomProject(lmprojPath);
        if (project.Build() != LoomProject.BuildResult.Success)
        {
            PrintDiagnostics(project.CompilationContext.DiagnosticContext);
            return null;
        }

        return project;
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
