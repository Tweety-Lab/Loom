using Loom.Common;
using Loom.Parser;
using Loom.Parser.Rules.Default;

namespace Loom.Tests;

public class DebugTest
{
    public const string TEST_SOURCE = @"
import Test2;

module Test
{
    // Test
    unsafe
    {

    }
}

module Test2
{
}
";

    [Fact]
    public void Test1()
    {
        CompilationContext context = new CompilationContext();
        context.Parse(TEST_SOURCE);

        foreach (var error in context.Exceptions)
            throw error;

        ProgramNode? root = context.RootNode;

        Assert.NotNull(root);
        Assert.True(root.Imports.Count == 1);
        Assert.Equal("Test2", root.Imports.First().ModuleName);

        Assert.True(root.Modules.Count == 2);
        Assert.Equal("Test", root.Modules.First().Name);

        Assert.True(root.Modules.First().Body.Contents.First() is UnsafeNode);
    }
}
