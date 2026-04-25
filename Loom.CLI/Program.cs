using Loom.Analyzer;
using Loom.Common;
using Loom.Parser;
using System.Diagnostics;

namespace Loom.CLI;

public class Program
{
    public const string TEST_SOURCE = @"
import Test;
import Test2;
import thjkgdfg;

module Test
{
    export void MyMethod()
    {
        return 123;
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
            Console.WriteLine(diagnostic.Message);
    }
}
