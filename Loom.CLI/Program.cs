using Loom.Analyzer;
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
    i32 MyMethod()
    {
        i32 i = Test();
        return i;
    }
}

module Base
{
    export i32 Test()
    {
        return 1 + 1;
    }
}
";

    static void Main(string[] args)
    {
        CompilationContext context = new CompilationContext();
        context.Parse(TEST_SOURCE).Analyze().EmitLIR();

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
