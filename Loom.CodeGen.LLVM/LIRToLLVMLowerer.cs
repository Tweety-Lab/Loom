using LLVMSharp;
using LLVMSharp.Interop;
using Loom.LIR;
using Loom.LIR.Objects;

namespace Loom.CodeGen.LLVM;

/// <summary>
/// This entire class is just a hack for testing.
/// </summary>
public class LIRToLLVMLowerer
{
    public LLVMModuleRef Lower(IEnumerable<LIRCompilationUnit> units)
    {
        var unit = units.First();

        string name = unit.MetaData.TryGetValue("Name", out var nameValue) ? nameValue : "Unknown";

        var ctx = new LLVMLoweringContext(name);

        foreach (var func in unit.Functions)
            LowerFunction(ctx, func);

        return ctx.Module;
    }

    private LLVMValueRef LowerFunction(LLVMLoweringContext ctx, LIRFunction func)
    {
        var functionType = LLVMTypeRef.CreateFunction(LIRTypeToLLVM(func.Type.ReturnType), Array.Empty<LLVMTypeRef>());

        var llvmFunc = ctx.Module.AddFunction(func.Name, functionType);

        ctx.Values[func] = llvmFunc;

        foreach (var block in func.Blocks)
            LowerBlock(ctx, llvmFunc, block);

        return llvmFunc;
    }

    private void LowerBlock(LLVMLoweringContext ctx, LLVMValueRef func, LIRBasicBlock block)
    {
        var llvmBlock = func.AppendBasicBlock(block.Name);

        ctx.Builder.PositionAtEnd(llvmBlock);

        foreach (var instr in block.Instructions)
            LowerInstruction(ctx, instr);

        if (block.Terminator != null)
            LowerInstruction(ctx, block.Terminator);
    }

    private LLVMValueRef? LowerInstruction(LLVMLoweringContext ctx, LIRInstruction instr)
    {
        LLVMValueRef? result = instr.OpCode.Name switch
        {
            "add" => ctx.Builder.BuildAdd(LowerOperand(ctx, instr.Operands[0]), LowerOperand(ctx, instr.Operands[1]), "addtmp"),

            "sub" => ctx.Builder.BuildSub(LowerOperand(ctx, instr.Operands[0]), LowerOperand(ctx, instr.Operands[1]), "subtmp"),

            "mul" => ctx.Builder.BuildMul(LowerOperand(ctx, instr.Operands[0]), LowerOperand(ctx, instr.Operands[1]), "multmp"),

            "div" => ctx.Builder.BuildSDiv(LowerOperand(ctx, instr.Operands[0]), LowerOperand(ctx, instr.Operands[1]), "divtmp"),

            "alloca" => ctx.Builder.BuildAlloca(LIRTypeToLLVM(instr.Result!.Type), instr.Result?.ToString() ?? "allocatmp"),

            "store" => ctx.Builder.BuildStore(LowerOperand(ctx, instr.Operands[0]), LowerOperand(ctx, instr.Operands[1])),

            "load" => ctx.Builder.BuildLoad2(LIRTypeToLLVM(instr.Result!.Type), LowerOperand(ctx, instr.Operands[0]), "loadtmp"),

            "call" => LowerCall(ctx, instr),

            "return" => instr.Operands.Count == 0 ? ctx.Builder.BuildRetVoid() : ctx.Builder.BuildRet(LowerOperand(ctx, instr.Operands[0])),


            _ => throw new NotSupportedException(instr.OpCode.Name)
        };

        if (instr.Result != null && result.HasValue)
            ctx.Values[instr.Result] = result.Value;

        return result;
    }

    private LLVMValueRef LowerOperand(LLVMLoweringContext ctx, LIRValue value)
    {
        if (value is LIRConstantIntValue c)
            return LLVMValueRef.CreateConstInt(LIRTypeToLLVM(c.Type), (ulong)c.Value, true);

        if (value is LIRFunction func)
            return ctx.Values[func];

        if (ctx.Values.TryGetValue(value, out var llvmValue))
            return llvmValue;

        throw new NotSupportedException($"Unknown operand: {value}");
    }

    private LLVMTypeRef LIRTypeToLLVM(LIRType type) => type switch
    {
        LIRIntType => LLVMTypeRef.Int32,
        LIRVoidType => LLVMTypeRef.Void,
        _ => throw new NotSupportedException($"Unknown type: {type}")
    };

    private LLVMValueRef LowerCall(LLVMLoweringContext ctx, LIRInstruction instr)
    {
        var callee = LowerOperand(ctx, instr.Operands[0]);
        var args = instr.Operands.Skip(1).Select(op => LowerOperand(ctx, op)).ToArray();

        var funcType = (LIRFunctionType)instr.Operands[0].Type;

        var returnType = LIRTypeToLLVM(funcType.ReturnType);

        return ctx.Builder.BuildCall2(returnType, callee, args, "calltmp");
    }
}
