using Loom.LIR;
using System;
using System.Collections.Generic;
using System.Text;

namespace Loom.CodeGen.LLVM.Emitters;

internal class InstructionEmitter : Emitter<LIRInstruction>
{
    /// <inheritdoc/>
    public InstructionEmitter(LLVMTranslationContext context) : base(context) { }

    /// <inheritdoc/>
    public override void Emit(LIRInstruction target)
    {
        Console.WriteLine($"Emitting Instruction: {target.OpCode.Name}");
    }
}
