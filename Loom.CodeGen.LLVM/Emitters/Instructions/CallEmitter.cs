
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

        if (instruction.Operands[0] is not LIRFunction funcValue)
            throw new InvalidOperationException("Call operand[0] must be an LIRFunctionValue.");

        if (!context.FunctionMap.TryGetValue(funcValue, out LLVMValueRef llvmFunc))
            throw new InvalidOperationException($"Function '{funcValue.Name}' has not been declared.");

        LLVMValueRef[] args = instruction.Operands.Skip(1).Select(context.ResolveValue).ToArray();

        LIRFunctionType lirFuncType = funcValue.Type;

        LLVMTypeRef[] paramTypes = lirFuncType.Parameters.Select(p => context.TypeMap[p.Type]).ToArray();

        LLVMTypeRef returnType = context.TypeMap[lirFuncType.ReturnType];
        LLVMTypeRef llvmFuncType = LLVMTypeRef.CreateFunction(returnType, paramTypes);

        bool isVoid = lirFuncType.ReturnType == LIRType.Void;
        string resultName = isVoid ? "" : ((LIRTempValue)instruction.Result).ID ?? ""; // Hack

        LLVMValueRef callResult = context.Builder.BuildCall2(llvmFuncType, llvmFunc, args, resultName);

        if (!isVoid && instruction.Result != null)
            context.ValueMap[instruction.Result] = callResult;
    }
}
