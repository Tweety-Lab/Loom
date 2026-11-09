
using CommandLine;

namespace Loom.CLI.Commands;

[Verb("build")]
internal class BuildCommand : Command
{
    [Option('p', "path")]
    public string? Path { get; set; }

    [Option('t', "target")]
    public string? Target { get; set; }

    /// <inheritdoc/>
    public override void Execute()
    {
        Console.WriteLine("Building...");
    }
}
