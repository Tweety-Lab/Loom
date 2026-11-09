
using Loom.Analyzer;
using Loom.Common;
using Loom.LIR;
using Loom.Parser;

namespace Loom.CLI;

/// <summary>
/// Represents an entire Loom Project.
/// </summary>
/// <remarks>
/// A Loom Project is a <c>.lmproj</c> file along with all <c>.loom</c> source files in the same directory.
/// </remarks>
public sealed class LoomProject
{
    public enum BuildResult
    {
        Success,
        Failure
    }

    /// <summary> The name of the <see cref="LoomProject"/>. </summary>
    public string Name { get; }

    /// <summary> Paths to all source files in the <see cref="LoomProject"/>. </summary>
    public string[] SourceFiles { get; }

    /// <summary> The <see cref="Loom.Common.CompilationContext"/> this <see cref="LoomProject"/> uses. </summary>
    public CompilationContext CompilationContext { get; } = new();

    /// <summary> Initializes a new instance of the <see cref="LoomProject"/> class. </summary>
    /// <param name="lmprojpath">Path to the <c>.lmproj</c> file.</param>
    public LoomProject(string lmprojpath)
    {
        if (!File.Exists(lmprojpath))
            throw new FileNotFoundException("The specified file does not exist.", lmprojpath);

        Name = Path.GetFileNameWithoutExtension(lmprojpath);
        CompilationContext.ProjectName = Name;

        string projectDirectory = Path.GetDirectoryName(Path.GetFullPath(lmprojpath))!;
        SourceFiles = Directory.GetFiles(projectDirectory, "*.loom", SearchOption.AllDirectories);

        // Add standard lib source files
        // Dumbest very temporary hack ever
        if (Name == "Loom.Standard")
            return;

        var standardLibrary = FindStandardLibrary();
        if (standardLibrary is not null)
            SourceFiles = SourceFiles.Concat(Directory.GetFiles(standardLibrary, "*.loom", SearchOption.AllDirectories)).ToArray();
    }

    private static string? FindStandardLibrary()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            var standardLibrary = Path.Combine(directory.FullName, "Loom.Standard");
            if (Directory.Exists(standardLibrary))
                return standardLibrary;
        }

        return null;
    }

    /// <summary> Builds the <see cref="LoomProject"/>. </summary>
    public BuildResult Build()
    {
        var diagnostics = CompilationContext.DiagnosticContext.Diagnostics;

        foreach (var file in SourceFiles)
            CompilationContext.Parse(File.ReadAllText(file), Path.GetFileNameWithoutExtension(file));

        if (diagnostics.Count > 0)
            return BuildResult.Failure;

        CompilationContext.Analyze();

        if (diagnostics.Count > 0)
            return BuildResult.Failure;

        CompilationContext.EmitLIR();

        return BuildResult.Success;
    }
}
