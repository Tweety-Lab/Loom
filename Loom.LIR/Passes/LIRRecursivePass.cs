
using Loom.LIR.Objects;

namespace Loom.LIR.Passes;

/// <summary>
/// A <see cref="LIRCompilationPass"/> that runs on an entire <see cref="LIRCompilationUnit"/> and it's contents recursively.
/// </summary>
public abstract class LIRRecursivePass : LIRCompilationPass
{
    /// <inheritdoc/>
    public override void Run(LIRCompilationUnit unit)
    {
        foreach (var func in unit.AllFunctions.ToList()) // Prevent enumeration issues
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
