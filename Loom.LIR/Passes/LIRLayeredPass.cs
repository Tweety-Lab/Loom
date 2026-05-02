using Loom.LIR.Objects;

namespace Loom.LIR.Passes;

/// <summary>
/// A <see cref="ICompilationPass"/> that runs on an entire <see cref="LIRCompilationUnit"/> and it's contents through a layered approach.
/// </summary>
/// <remarks>
/// Layered paths first run on all functions, then on all blocks, and finally on all instructions. Unlike recursive paths which runs on a function, it's blocks, and it's instructions then
/// moves on to the next function.
/// </remarks>
public abstract class LIRLayeredPass : ICompilationPass
{
    /// <inheritdoc/>
    public virtual void Run(LIRCompilationUnit unit)
    {
        foreach (var func in unit.Functions)
            RunOnFunction(func);

        foreach (var func in unit.Functions)
            foreach (var block in func.Blocks)
                RunOnBlock(block);

        foreach (var func in unit.Functions)
            foreach (var block in func.Blocks)
                foreach (var inst in block.Instructions.ToList()) // Prevent enumeration issues
                    RunOnInstruction(inst);
    }

    protected virtual void RunOnFunction(LIRFunction func) { }

    protected virtual void RunOnBlock(LIRBasicBlock block) { }

    protected virtual void RunOnInstruction(LIRInstruction inst) { }
}

