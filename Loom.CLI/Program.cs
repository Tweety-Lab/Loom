using Loom.Analyzer;
using Loom.Common;
using Loom.Parser;

namespace Loom.CLI;

public class Program
{
    public const string TEST_SOURCE = @"
import Test;
import Test2;

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
        context.Parse(TEST_SOURCE);

        foreach (var error in context.Exceptions)
            throw error;

        context.Analyze();
    }
}
