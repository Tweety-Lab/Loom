using LLVMSharp.Interop;
using Loom.Analyzer;
using Loom.CodeGen.LLVM;
using Loom.Common;
using Loom.Common.Diagnostics;
using Loom.LIR;
using Loom.LIR.Objects;
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
        ReturnTrue();

        i32 i = Add(10, 20);
        i32 x = Add(i, 1);
        return x;
    }
}

module Base
{
    export i32 Add(i32 first, i32 second)
    {
        return first + second;
    }

    export bool ReturnTrue()
    {
        return true;
    }
}
";

    static void Main(string[] args)
    {
        CompilationContext context = new CompilationContext();
        context.Parse(TEST_SOURCE).Analyze().EmitLIR();

        LIRCompilationUnit unit = context.CompilationUnits.First();

        LLVMTranslatorPass translator = new LLVMTranslatorPass();
        translator.Run(unit);

        string llvmIr = translator.Result.PrintToString();
        Console.WriteLine("===== LLVM RESULT =====");
        Console.WriteLine(llvmIr);

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
