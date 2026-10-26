
using LLVMSharp.Interop;
using Loom.LIR;
using Loom.LIR.Objects;
using Loom.LIR.OpCodes;

namespace Loom.CodeGen.LLVM.Emitters.Instructions;

[InstructionEmitter]
internal class CallEmitter : IInstructionEmitter
{
    /// <inheritdoc/>
    public LIROpCode TargetOpCode => LIROpCode.Call;

    /// <inheritdoc/>
    public void Emit(LIRInstruction instruction, LLVMTranslationContext context)
    {
        if (instruction.Operands.Count < 1)
            throw new InvalidOperationException("Call requires at least a function operand.");

        var target = instruction.Operands[0];
        var lirFuncType = target.Type switch
        {
            LIRFunctionType direct => direct,
            LIRPointerType { PointeeType: LIRFunctionType indirect } => indirect,
            _ => throw new InvalidOperationException("Call operand[0] must be a function or function pointer.")
        };

        LLVMValueRef llvmFunc = context.ResolveValue(target);

        LLVMValueRef[] args = instruction.Operands.Skip(1).Select(context.ResolveValue).ToArray();

        LLVMTypeRef[] paramTypes = lirFuncType.Parameters.Select(p => context.ResolveType(p.Type)).ToArray();

        LLVMTypeRef returnType = context.ResolveType(lirFuncType.ReturnType);
        LLVMTypeRef llvmFuncType = LLVMTypeRef.CreateFunction(returnType, paramTypes);

        bool isVoid = lirFuncType.ReturnType == LIRType.Void;
        string resultName = isVoid ? "" : (instruction.Result as LIRTempValue)?.ID ?? ""; // Hack

        LLVMValueRef callResult = context.Builder.BuildCall2(llvmFuncType, llvmFunc, args, resultName);

        if (!isVoid && instruction.Result != null)
            context.ValueMap[instruction.Result] = callResult;
    }
}
