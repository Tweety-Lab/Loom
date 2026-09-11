
using Loom.LIR.Objects;

namespace Loom.LIR.Passes;

/// <summary>
/// A <see cref="ICompilationPass"/> that runs on an entire <see cref="LIRCompilationUnit"/> and it's contents recursively.
/// </summary>
public abstract class LIRRecursivePass : ICompilationPass
{
    /// <inheritdoc/>
    public virtual void Run(LIRCompilationUnit unit)
    {
        foreach (var func in unit.AllFunctions.ToList())
            RunOnFunction(func);
    }

    protected virtual void RunOnFunction(LIRFunction func)
    {
        foreach (var block in func.Blocks)
            RunOnBlock(block);
    }

    protected virtual void RunOnBlock(LIRBasicBlock block)
    {
        foreach (var inst in block.Instructions.ToList()) // Prevent enumeration issues
            RunOnInstruction(inst);
    }

    protected virtual void RunOnInstruction(LIRInstruction inst) { }
}
