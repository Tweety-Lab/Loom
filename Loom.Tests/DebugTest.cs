using Loom.Common;
using Loom.Parser;
using Loom.Parser.AST;

namespace Loom.Tests;

public class DebugTest
{
    public const string TEST_SOURCE = @"
import Test2;

module Test
{
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

        ProgramNode? root = context.RootNode;

        Assert.NotNull(root);
        Assert.True(root.Modules.Count == 2);
        Assert.Equal(root.Modules.First().Name, "Test");

        Assert.Equal(root.Imports.First().ModuleName, "Test2");
        Assert.NotNull(root.Modules.First().Body.Contents.Find(x => x is UnsafeNode));
    }
}
