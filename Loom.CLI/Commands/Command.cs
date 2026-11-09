namespace Loom.CLI.Commands;

/// <summary>
/// Base class for all CLI commands.
/// </summary>
internal abstract class Command
{
    /// <summary> Called when the command is executed. </summary>
    public abstract void Execute();
}
