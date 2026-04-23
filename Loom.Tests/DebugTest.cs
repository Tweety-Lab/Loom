using Loom.Analyzer;
using Loom.Analyzer.Symbols;
using Loom.Common;
using Loom.Parser;
using Loom.Parser.AST;
using Loom.Parser.Rules.Default;

namespace Loom.Tests;

public class DebugTest
{
    public const string TEST_SOURCE = @"
import Test2;

module Test
{
    // Yapyapyapyap
    unsafe
    {

    }

    module Nested
    {

    }
}

module Test2
{
}
";

    [Fact]
    public void SweepTest()
    {
        CompilationContext context = new CompilationContext();
        context.Parse(TEST_SOURCE).Analyse();

        foreach (var error in context.Exceptions)
            throw error;

        ProgramNode? root = context.RootNode;

        Assert.NotNull(root);

        Assert.True(root.Imports.Count == 1);
        Assert.Equal("Test2", root.Imports.First().ModuleName);

        Assert.True(root.Modules.Count == 2);
        Assert.Equal("Test", root.Modules.First().Name);

        Assert.True(root.Modules.First().Body.Contents.First() is UnsafeNode);

        Dictionary<ASTNode, SymbolTable>? symbolMap = context.SymbolMap;

        Assert.NotNull(symbolMap);

        Assert.NotNull(symbolMap[root.Modules.First()].Resolve("Nested"));
    }
}
