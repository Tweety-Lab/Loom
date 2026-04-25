using Loom.Analyzer;
using Loom.Analyzer.Symbols;
using Loom.Common;
using Loom.Common.Diagnostics;
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

    public const string RETURN_TYPE_MISMATCH_SOURCE = @"
module Test
{
    void MyMethod()
    {
        return 123;
    }
}
";

    public const string UNRESOLVED_IMPORT_SOURCE = @"
import NonExistent;

module Test
{
}
";

    public const string UNRESOLVED_TYPE_SOURCE = @"
module Test
{
    FakeType MyMethod()
    {
        return 123;
    }
}
";

    private (ProgramNode root, CompilationContext context) ParseAndAnalyze(string source = TEST_SOURCE)
    {
        CompilationContext context = new CompilationContext();
        context.Parse(source).Analyze();
        return (context.RootNode!, context);
    }

    [Fact]
    public void Parse_TwoImports()
    {
        var (root, _) = ParseAndAnalyze();
        Assert.Equal(2, root.Imports.Count);
        Assert.Equal("Test", root.Imports[0].ModuleName);
        Assert.Equal("Test2", root.Imports[1].ModuleName);
    }

    [Fact]
    public void Parse_TwoModules()
    {
        var (root, _) = ParseAndAnalyze();
        Assert.Equal(2, root.Modules.Count);
        Assert.Equal("Test", root.Modules[0].Name);
        Assert.Equal("Test2", root.Modules[1].Name);
    }

    [Fact]
    public void Parse_MethodHasExportModifier()
    {
        var (root, _) = ParseAndAnalyze();
        var method = (MethodDefinitionNode)root.Modules[0].Body.Contents.First();
        Assert.Contains(method.Modifiers, m => m.Type == Token.TokenType.Export);
    }

    [Fact]
    public void Parse_MethodHasReturnStatement()
    {
        var (root, _) = ParseAndAnalyze();
        var method = (MethodDefinitionNode)root.Modules[0].Body.Contents.First();
        Assert.Single(method.Body.Contents);
        Assert.IsType<ReturnStatementNode>(method.Body.Contents.First());
    }

    [Fact]
    public void Parse_ReturnStatementHasNumberLiteral()
    {
        var (root, _) = ParseAndAnalyze();
        var method = (MethodDefinitionNode)root.Modules[0].Body.Contents.First();
        var returnStatement = (ReturnStatementNode)method.Body.Contents.First();
        var literal = Assert.IsType<NumberLiteralNode>(returnStatement.Expression);
        Assert.Equal("123", literal.Value);
    }

    [Fact]
    public void Analyze_ModulesRegisteredInSymbolTable()
    {
        var (root, context) = ParseAndAnalyze();
        var symbolMap = context.SymbolMap!;

        Assert.IsType<ModuleSymbol>(symbolMap[root].Resolve("Test"));
        Assert.IsType<ModuleSymbol>(symbolMap[root].Resolve("Test2"));
    }

    [Fact]
    public void Analyze_MethodRegisteredInSymbolTable()
    {
        var (root, context) = ParseAndAnalyze();
        var symbolMap = context.SymbolMap!;

        var methodSymbol = symbolMap[root.Modules[0]].Resolve("MyMethod");
        Assert.IsType<MethodDefinitionSymbol>(methodSymbol);
        Assert.Equal(TypeSymbol.Type.I32, ((MethodDefinitionSymbol)methodSymbol).ReturnType.KnownType);
    }

    [Fact]
    public void Analyze_UnresolvedImport_ReportsDiagnostic()
    {
        var (_, context) = ParseAndAnalyze(UNRESOLVED_IMPORT_SOURCE);
        Assert.Contains(context.DiagnosticContext.Diagnostics, d => d.Level == Diagnostic.DiagnosticLevel.Error);
    }

    [Fact]
    public void Analyze_UnresolvedType_ReportsDiagnostic()
    {
        var (_, context) = ParseAndAnalyze(UNRESOLVED_TYPE_SOURCE);
        Assert.Contains(context.DiagnosticContext.Diagnostics, d => d.Level == Diagnostic.DiagnosticLevel.Error);
    }
}