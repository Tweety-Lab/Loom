using Loom.Analyzer;
using Loom.Analyzer.Symbols;
using Loom.Common;
using Loom.Parser;
using Loom.Parser.AST;
using Loom.Parser.Rules.Default;
using Loom.Parser.Tokenizer;

namespace Loom.Tests;

public class DebugTest
{
    public const string TEST_SOURCE = @"
import Test;
import Test2;

module Test
{
    export void MyMethod()
    {
        i32 i = 0;
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
        context.Parse(TEST_SOURCE);

        foreach (var error in context.Exceptions)
            throw error;

        context.Analyze();

        ProgramNode? root = context.RootNode;
        
        Assert.NotNull(root);

        Assert.True(root.Imports.Count == 2);
        Assert.Equal("Test", root.Imports.First().ModuleName);

        Assert.True(root.Modules.Count == 2);
        Assert.Equal("Test", root.Modules.First().Name);

        Assert.True(((MethodDefinitionNode)root.Modules.First().Body.Contents.First()).Modifiers.Any(m => m.Type == Token.TokenType.Export));

        Dictionary<ASTNode, SymbolTable>? symbolMap = context.SymbolMap;

        Assert.NotNull(symbolMap);

        Assert.Single(symbolMap[root.Modules.First()].Symbols);

        Assert.True(symbolMap[root.Modules.First()].Resolve("MyMethod") is MethodDefinitionSymbol);
        Assert.Equal(TypeSymbol.Type.Void, ((MethodDefinitionSymbol)symbolMap[root.Modules.First()].Resolve("MyMethod")).ReturnType.KnownType);
    }
}
