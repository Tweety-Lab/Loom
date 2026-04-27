using Loom.Analyzer;
using Loom.Common;
using Loom.Common.Diagnostics;
using Loom.Parser;
using System.Diagnostics;

namespace Loom.CLI;

public class Program
{
    public const string TEST_SOURCE = @"
import Test;
import Test2;

module Test
{
    export i32 MyMethod()
    {
        return Test();
    }

    i32 Test()
    {
        return 1;
    }
}

module Test2
{
}
";

    static void Main(string[] args)
    {
        CompilationContext context = new CompilationContext();
        context.Parse(TEST_SOURCE).Analyze();

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
