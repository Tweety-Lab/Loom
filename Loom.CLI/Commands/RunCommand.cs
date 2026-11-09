using Loom.Analyzer;
using Loom.CLI.JustInTime;
using CommandLine;

namespace Loom.CLI.Commands;

[Verb("run")]
internal class RunCommand : Command
{
    [Option('p', "path")]
    public string Path { get; set; } = "./";

    /// <inheritdoc/>
    public override void Execute()
    {
        BuildCommand builder = new BuildCommand();
        builder.Path = Path;

        LoomProject? project = builder.Build();
        if (project == null)
            throw new Exception("Build Failed.");

        LLVMJITCompiler llvm = new LLVMJITCompiler();
        if (llvm.TryInitialize(project))
        {
            if (project.CompilationContext.AnalysisContext.EntryPoint == null)
                return;

            if (llvm.TryExecute(project.CompilationContext.AnalysisContext.EntryPoint!, out IJITResult? result))
            {
                if (result == null)
                    return;

                Console.WriteLine($"Result: {result.ToInt64()}");
            }
        }
    }
}
