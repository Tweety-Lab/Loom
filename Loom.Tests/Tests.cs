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

    public const string METHOD_CALL_SOURCE = @"
module Test
{
    void Caller()
    {
        MyMethod();
    }

    void MyMethod()
    {
    }
}
";

    public const string ADD_EXPRESSION_SOURCE = @"
module Test
{
    i32 MyMethod()
    {
        return 1 + 2;
    }
}
";

    public const string LEFT_ASSOCIATIVE_SOURCE = @"
module Test
{
    i32 MyMethod()
    {
        return 1 + 2 + 3;
    }
}
";

    public const string PRECEDENCE_SOURCE = @"
module Test
{
    i32 MyMethod()
    {
        return 1 + 2 * 3;
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
        Assert.Equal("Test", root.Imports[0].ModuleName.BaseName);
        Assert.Equal("Test2", root.Imports[1].ModuleName.BaseName);
    }

    [Fact]
    public void Parse_TwoModules()
    {
        var (root, _) = ParseAndAnalyze();
        Assert.Equal(2, root.Modules.Count);
        Assert.Equal("Test", root.Modules[0].Name.BaseName);
        Assert.Equal("Test2", root.Modules[1].Name.BaseName);
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
    public void Analyze_ModulesRegisteredInBinder()
    {
        var (root, context) = ParseAndAnalyze();
        var symbolMap = context.SymbolMap!;

        Assert.IsType<ModuleSymbol>(symbolMap[root].Lookup("Test")?.First());
        Assert.IsType<ModuleSymbol>(symbolMap[root].Lookup("Test2")?.First());
    }

    [Fact]
    public void Analyze_MethodRegisteredInBinder()
    {
        var (root, context) = ParseAndAnalyze();
        var symbolMap = context.SymbolMap!;

        var methodSymbol = symbolMap[root.Modules[0]].Lookup("MyMethod")?.First();
        Assert.IsType<MethodDefinitionSymbol>(methodSymbol);
        Assert.Equal(TypeSymbol.KnownType.I32, ((MethodDefinitionSymbol)methodSymbol).ReturnType.Type);
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

    [Fact]
    public void Parse_MethodCallStatement()
    {
        var (root, _) = ParseAndAnalyze(METHOD_CALL_SOURCE);
        var caller = (MethodDefinitionNode)root.Modules[0].Body.Contents.First();
        var statement = Assert.IsType<ExpressionStatementNode>(caller.Body.Contents.First());
        var call = Assert.IsType<CallExpressionNode>(statement.Expression);
        Assert.Equal("MyMethod", call.MethodName.BaseName);
        Assert.Empty(call.Arguments);
    }

    [Fact]
    public void Parse_BinaryExpression_Addition()
    {
        var (root, _) = ParseAndAnalyze(ADD_EXPRESSION_SOURCE);
        var method = (MethodDefinitionNode)root.Modules[0].Body.Contents.First();
        var returnStatement = (ReturnStatementNode)method.Body.Contents.First();

        var binary = Assert.IsType<BinaryExpressionNode>(returnStatement.Expression);

        var left = Assert.IsType<NumberLiteralNode>(binary.Left);
        var right = Assert.IsType<NumberLiteralNode>(binary.Right);

        Assert.Equal("1", left.Value);
        Assert.Equal("2", right.Value);
        Assert.Equal(Token.TokenType.Plus, binary.Operator.Type);
    }

    [Fact]
    public void Parse_BinaryExpression_LeftAssociative()
    {
        var (root, _) = ParseAndAnalyze(LEFT_ASSOCIATIVE_SOURCE);
        var method = (MethodDefinitionNode)root.Modules[0].Body.Contents.First();
        var returnStatement = (ReturnStatementNode)method.Body.Contents.First();

        var outer = Assert.IsType<BinaryExpressionNode>(returnStatement.Expression);

        var inner = Assert.IsType<BinaryExpressionNode>(outer.Left);
        var right = Assert.IsType<NumberLiteralNode>(outer.Right);

        Assert.Equal("3", right.Value);

        var innerLeft = Assert.IsType<NumberLiteralNode>(inner.Left);
        var innerRight = Assert.IsType<NumberLiteralNode>(inner.Right);

        Assert.Equal("1", innerLeft.Value);
        Assert.Equal("2", innerRight.Value);
    }

    [Fact]
    public void Parse_BinaryExpression_Precedence()
    {
        var (root, _) = ParseAndAnalyze(PRECEDENCE_SOURCE);
        var method = (MethodDefinitionNode)root.Modules[0].Body.Contents.First();
        var returnStatement = (ReturnStatementNode)method.Body.Contents.First();

        var add = Assert.IsType<BinaryExpressionNode>(returnStatement.Expression);

        var left = Assert.IsType<NumberLiteralNode>(add.Left);
        Assert.Equal("1", left.Value);

        var mult = Assert.IsType<BinaryExpressionNode>(add.Right);
        Assert.Equal(Token.TokenType.Star, mult.Operator.Type);

        var multLeft = Assert.IsType<NumberLiteralNode>(mult.Left);
        var multRight = Assert.IsType<NumberLiteralNode>(mult.Right);

        Assert.Equal("2", multLeft.Value);
        Assert.Equal("3", multRight.Value);
    }
}