using Loom.LIR.Objects;

namespace Loom.LIR.Passes;

/// <summary>
/// A <see cref="ICompilationPass"/> that runs on an entire <see cref="LIRCompilationUnit"/> and its contents through a layered approach.
/// </summary>
/// <remarks>
/// Layered passes first run on all functions, then on all blocks, and finally on all instructions. Unlike recursive passes which run on a function, it's blocks, and it's instructions then
/// moves on to the next function, layering reports every function before any block or instruction is visited. This ordering guarantees call targets are seen by a pass (e.g. an LLVM
/// translator that emits declarations up front) before they are referenced from a block that was emitted earlier.
/// </remarks>
public abstract class LIRLayeredPass : ICompilationPass
{
    /// <inheritdoc/>
    public virtual void Run(LIRCompilationUnit unit)
    {
        var allFunctions = unit.AllFunctions.ToList();

        foreach (var func in allFunctions)
            RunOnFunction(func);

        foreach (var func in allFunctions)
            foreach (var block in func.Blocks)
                RunOnBlock(block);

        foreach (var func in allFunctions)
            foreach (var block in func.Blocks)
                foreach (var inst in block.Instructions.ToList()) // Prevent enumeration issues
                    RunOnInstruction(block, inst);
    }

    protected virtual void RunOnFunction(LIRFunction func) { }

    protected virtual void RunOnBlock(LIRBasicBlock block) { }

    protected virtual void RunOnInstruction(LIRBasicBlock block, LIRInstruction inst) { }
}