using LLVMSharp.Interop;
using Loom.LIR.Objects;

namespace Loom.CodeGen.LLVM;

public class LIRToLLVMLowerer
{
    public LLVMModuleRef Lower(IEnumerable<LIRCompilationUnit> units)
    {
        List<LLVMModuleRef> modules = new();
        foreach (var unit in units)
            modules.Add(LowerUnit(unit));

        return modules.First();
    }

    private LLVMModuleRef LowerUnit(LIRCompilationUnit unit)
    {
        string name = unit.MetaData.TryGetValue("Name", out string? nameValue) ? nameValue : "Unknown";
        var llvmModule = LLVMModuleRef.CreateWithName(name);

        foreach (var func in unit.Functions)
            LowerFunction(llvmModule, func);

        return llvmModule;
    }

    private LLVMValueRef LowerFunction(LLVMModuleRef module, LIRFunction func)
    {
        // Create new LLVM Function
        var baseFunc = LLVMTypeRef.CreateFunction(LLVMTypeRef.Void, []);
        var llvmFunc = module.AddFunction(func.Name, baseFunc);

        return llvmFunc;
    }


}
