using LLVMSharp.Interop;
using Loom.Analyzer;
using Loom.CodeGen.LLVM;
using Loom.Common;
using Loom.Common.Diagnostics;
using Loom.LIR;
using Loom.Parser;

namespace Loom.CLI;

public class Program
{
    public const string TEST_SOURCE = @"
import Base;

module Consumer
{   
    // Entry Point
    export i32 Main()
    {
        i32 i = Test() + 5;
        return i;
    }
}

module Base
{
    i32 Test()
    {
        return 1 + 1;
    }
}
";

    static void Main(string[] args)
    {
        CompilationContext context = new CompilationContext();
        context.Parse(TEST_SOURCE).Analyze().EmitLIR();

        LLVM.LinkInMCJIT();
        LLVM.InitializeNativeTarget();
        LLVM.InitializeNativeAsmPrinter();
        LLVM.InitializeNativeAsmParser();

        LIRToLLVMLowerer lowerer = new LIRToLLVMLowerer();
        LLVMModuleRef module = lowerer.Lower(context.CompilationUnits);

        Console.WriteLine(module.PrintToString());

        LLVMValueRef entryPoint = module.GetNamedFunction(context.AnalysisContext.EntryPoint!.FullyQualifiedName!);

        var engine = module.CreateExecutionEngine();
        var result = engine.RunFunction(entryPoint, Array.Empty<LLVMGenericValueRef>());

        unsafe
        {
            int value = unchecked((int)LLVM.GenericValueToInt(result, 1));
            Console.WriteLine($"{entryPoint.Name} Result: {value}");
        }

        foreach (var diagnostic in context.DiagnosticContext.Diagnostics)
        {
            Console.ForegroundColor = diagnostic.Level switch
            {
                Diagnostic.DiagnosticLevel.Error => ConsoleColor.Red,
                Diagnostic.DiagnosticLevel.Warning => ConsoleColor.Yellow,
                _ => ConsoleColor.White
            };

            Console.WriteLine(diagnostic.Message);
        }

        Console.ResetColor();
    }
}
