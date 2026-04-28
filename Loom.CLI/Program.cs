using Loom.Analyzer;
using Loom.Analyzer.Symbols;
using Loom.Common;
using Loom.Common.Diagnostics;
using Loom.Parser;
using Loom.Parser.Rules.Default;
using System.Diagnostics;

namespace Loom.CLI;

public class Program
{
    public const string TEST_SOURCE = @"
import Test;

module Test
{
    export i32 MyMethod()
    {
        i32 x = Test();
        return x;
    }

    i32 Test()
    {
        return 1 + 1;
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
