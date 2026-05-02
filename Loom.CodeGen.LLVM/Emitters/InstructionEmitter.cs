using Loom.CodeGen.LLVM.Emitters.Instructions;
using Loom.Common.Reflection;
using Loom.LIR;
using System.Reflection;

namespace Loom.CodeGen.LLVM.Emitters;

internal class InstructionEmitter : Emitter<LIRInstruction>
{
    /// <inheritdoc/>
    public InstructionEmitter(LLVMTranslationContext context) : base(context) { }

    public void Emit(LIRBasicBlock block, LIRInstruction target)
    {
        Context.Builder.PositionAtEnd(Context.BlockMap[block]);

        List<IInstructionEmitter> emitters = LoomReflection.InstansiateAllWithAttribute<InstructionEmitterAttribute>(Assembly.GetExecutingAssembly()).Cast<IInstructionEmitter>().ToList();

        foreach (var emitter in emitters)
            if (target.OpCode == emitter.TargetOpCode)
                emitter.Emit(target, Context);
    }

    /// <inheritdoc/>
    [Obsolete("Use Emit(block, instruction)")]
    public override void Emit(LIRInstruction target) => throw new NotSupportedException("Use Emit(block, instruction)"); // HACK
}
