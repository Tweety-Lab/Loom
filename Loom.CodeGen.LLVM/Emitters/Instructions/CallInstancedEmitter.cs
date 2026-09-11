using LLVMSharp.Interop;
using Loom.LIR;
using Loom.LIR.Objects;
using Loom.LIR.OpCodes;

namespace Loom.CodeGen.LLVM.Emitters.Instructions;

/// <summary> Emits an instance call where <c>Operands[1]</c> is the receiver (this) passed as the first argument. </summary>
[InstructionEmitter]
internal class CallInstancedEmitter : IInstructionEmitter
{
    /// <inheritdoc/>
    public LIROpCode TargetOpCode => LIROpCode.CallInstanced;

    /// <inheritdoc/>
    public void Emit(LIRInstruction instruction, LLVMTranslationContext context)
    {
        if (instruction.Operands.Count < 2)
            throw new InvalidOperationException("CallInstanced requires at least a function and a receiver operand.");

        if (instruction.Operands[0] is not LIRFunction funcValue)
            throw new InvalidOperationException("CallInstanced operand[0] must be an LIRFunctionValue.");

        if (!context.FunctionMap.TryGetValue(funcValue, out LLVMValueRef llvmFunc))
            throw new InvalidOperationException($"Function '{funcValue.Name}' has not been declared.");

        LLVMValueRef receiver = context.ResolveValue(instruction.Operands[1]);
        LLVMValueRef[] args = instruction.Operands.Skip(2).Select(context.ResolveValue).ToArray();

        LIRFunctionType lirFuncType = funcValue.Type;

        LLVMTypeRef[] paramTypes = lirFuncType.Parameters.Select(p => context.ResolveType(p.Type)).ToArray();

        LLVMTypeRef returnType = context.ResolveType(lirFuncType.ReturnType);
        LLVMTypeRef llvmFuncType = LLVMTypeRef.CreateFunction(returnType, paramTypes);

        bool isVoid = lirFuncType.ReturnType == LIRType.Void;
        string resultName = isVoid ? "" : ((LIRTempValue)instruction.Result).ID ?? ""; // Hack

        LLVMValueRef[] callArgs = new LLVMValueRef[args.Length + 1];
        callArgs[0] = receiver;
        Array.Copy(args, 0, callArgs, 1, args.Length);

        LLVMValueRef callResult = context.Builder.BuildCall2(llvmFuncType, llvmFunc, callArgs, resultName);

        if (!isVoid && instruction.Result != null)
            context.ValueMap[instruction.Result] = callResult;
    }
}