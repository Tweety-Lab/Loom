
using LLVMSharp.Interop;
using Loom.Analyzer.Symbols;
using Loom.CodeGen.LLVM;
using Loom.LIR;
using Loom.LIR.Objects;

namespace Loom.CLI.JustInTime;

public sealed class LLVMJITCompiler : IJITCompiler
{
    /// <summary> The project being compiled. </summary>
    /// <remarks> This is set by <see cref="TryInitialize(LoomProject)"/>.</remarks>
    public LoomProject? Project { get; private set; }

    /// <inheritdoc/>
    public bool TryInitialize(LoomProject project)
    {
        if (LLVM.InitializeNativeTarget() != 0)
            return false;

        if (LLVM.InitializeNativeAsmPrinter() != 0)
            return false;

        if (LLVM.InitializeNativeAsmParser() != 0)
            return false;

        Project = project;

        return true;
    }

    /// <inheritdoc/>
    public bool TryExecute(MethodSymbol method, out IJITResult? jitResult)
    {
        jitResult = null;
        if (Project == null)
            return false;

        LIRCompilationUnit unit = Project.CompilationContext.CompilationUnits.First(unit => unit.AllFunctions.Any(function => function.Name == method.FullyQualifiedName));

        LLVMTranslatorPass translator = new LLVMTranslatorPass();
        translator.Run(unit);

        LLVMExecutionEngineRef engine = translator.Result.CreateExecutionEngine();
        LLVMValueRef main = translator.Result.GetNamedFunction(method.FullyQualifiedName);
        LLVMGenericValueRef result = engine.RunFunction(main, []);

        jitResult = new LLVMJITResult { Value = result };

        Console.WriteLine("===== LLVM IR =====");
        Console.WriteLine(translator.Result.ToString());

        return true;
    }
}


public readonly struct LLVMJITResult : IJITResult
{
    public LLVMGenericValueRef Value { get; init; }

    /// <inheritdoc/>
    public unsafe int ToInt32() => (int)LLVM.GenericValueToInt(Value, 1);
}