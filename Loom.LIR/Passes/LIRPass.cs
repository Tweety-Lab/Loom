
using Loom.LIR.Objects;

namespace Loom.LIR.Passes;

/// <summary>
/// Base class for all Loom Intermediate Representation passes.
/// </summary>
public abstract class LIRPass : ICompilationPass
{
    /// <inheritdoc/>
    public virtual void Run(LIRCompilationUnit unit)
    {
        foreach (var func in unit.Functions)
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
