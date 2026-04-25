using Loom.Analyzer;
using Loom.Analyzer.Symbols;
using Loom.Common;
using Loom.Parser;
using Loom.Parser.AST;
using Loom.Parser.Rules.Default;
using Loom.Parser.Tokenizer;

namespace Loom.Tests;

public class Tests
{
    public const string TEST_SOURCE = @"
import Test;
import Test2;

module Test
{
    export i32 MyMethod()
    {
        return 123;
    }
}

module Test2
{
}
";

    private ProgramNode ParseAndAnalyze()
    {
        CompilationContext context = new CompilationContext();
        context.Parse(TEST_SOURCE);

        foreach (var diagnostic in context.DiagnosticContext.Diagnostics)
            Console.WriteLine(diagnostic.Message);

        context.Analyze();

        return context.RootNode!;
    }

    [Fact]
    public void Parse_TwoImports()
    {
        var root = ParseAndAnalyze();
        Assert.Equal(2, root.Imports.Count);
        Assert.Equal("Test", root.Imports[0].ModuleName);
        Assert.Equal("Test2", root.Imports[1].ModuleName);
    }

    [Fact]
    public void Parse_TwoModules()
    {
        var root = ParseAndAnalyze();
        Assert.Equal(2, root.Modules.Count);
        Assert.Equal("Test", root.Modules[0].Name);
        Assert.Equal("Test2", root.Modules[1].Name);
    }

    [Fact]
    public void Parse_MethodHasExportModifier()
    {
        var root = ParseAndAnalyze();
        var method = (MethodDefinitionNode)root.Modules[0].Body.Contents.First();
        Assert.Contains(method.Modifiers, m => m.Type == Token.TokenType.Export);
    }

    [Fact]
    public void Parse_MethodHasReturnStatement()
    {
        var root = ParseAndAnalyze();
        var method = (MethodDefinitionNode)root.Modules[0].Body.Contents.First();
        Assert.Single(method.Body.Contents);
        Assert.IsType<ReturnStatementNode>(method.Body.Contents.First());
    }

    [Fact]
    public void Parse_ReturnStatementHasNumberLiteral()
    {
        var root = ParseAndAnalyze();
        var method = (MethodDefinitionNode)root.Modules[0].Body.Contents.First();
        var returnStatement = (ReturnStatementNode)method.Body.Contents.First();
        var literal = Assert.IsType<NumberLiteralNode>(returnStatement.Expression);
        Assert.Equal("123", literal.Value);
    }

    [Fact]
    public void Analyze_ModulesRegisteredInSymbolTable()
    {
        CompilationContext context = new CompilationContext();
        context.Parse(TEST_SOURCE);
        context.Analyze();

        var root = context.RootNode!;
        var symbolMap = context.SymbolMap!;

        Assert.IsType<ModuleSymbol>(symbolMap[root].Resolve("Test"));
        Assert.IsType<ModuleSymbol>(symbolMap[root].Resolve("Test2"));
    }

    [Fact]
    public void Analyze_MethodRegisteredInSymbolTable()
    {
        CompilationContext context = new CompilationContext();
        context.Parse(TEST_SOURCE);
        context.Analyze();

        var root = context.RootNode!;
        var symbolMap = context.SymbolMap!;

        var methodSymbol = symbolMap[root.Modules[0]].Resolve("MyMethod");
        Assert.IsType<MethodDefinitionSymbol>(methodSymbol);
        Assert.Equal(TypeSymbol.Type.Void, ((MethodDefinitionSymbol)methodSymbol).ReturnType.KnownType);
    }
}