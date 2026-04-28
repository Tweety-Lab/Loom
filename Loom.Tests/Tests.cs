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

    public const string BINDING_SOURCE = @"
import Test2;

module Test
{
    i32 MyMethod()
    {
        return Helper();
    }

    i32 Helper()
    {
        return 42;
    }
}

module Test2
{
}
";

    public const string VARIABLE_DECLARATION_SOURCE = @"
module Test
{
    i32 MyMethod()
    {
        i32 x = 42;
        return x;
    }
}
";

    public const string VARIABLE_EXPRESSION_SOURCE = @"
module Test
{
    i32 MyMethod()
    {
        i32 x = 1 + 2;
        i32 y = x + 3;
        return y;
    }
}
";

    private (ProgramNode root, CompilationContext context) ParseAndAnalyze(string source = TEST_SOURCE)
    {
        CompilationContext context = new CompilationContext();
        context.Parse(source).Analyze();
        return (context.SyntaxTrees!.First(), context);
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
        var method = (MethodDeclarationNode)root.Modules[0].Body.Contents.First();
        Assert.Contains(method.Modifiers, m => m.Type == Token.TokenType.Export);
    }

    [Fact]
    public void Parse_MethodHasReturnStatement()
    {
        var (root, _) = ParseAndAnalyze();
        var method = (MethodDeclarationNode)root.Modules[0].Body.Contents.First();
        Assert.Single(method.Body.Contents);
        Assert.IsType<ReturnStatementNode>(method.Body.Contents.First());
    }

    [Fact]
    public void Parse_ReturnStatementHasNumberLiteral()
    {
        var (root, _) = ParseAndAnalyze();
        var method = (MethodDeclarationNode)root.Modules[0].Body.Contents.First();
        var returnStatement = (ReturnStatementNode)method.Body.Contents.First();
        var literal = Assert.IsType<NumberLiteralNode>(returnStatement.Expression);
        Assert.Equal("123", literal.Value);
    }

    [Fact]
    public void Analyze_ModulesRegisteredInBinder()
    {
        var (root, context) = ParseAndAnalyze();
        var symbolMap = context.AnalysisContext.Binders;

        Assert.IsType<ModuleSymbol>(symbolMap[root].Lookup("Test")?.First());
        Assert.IsType<ModuleSymbol>(symbolMap[root].Lookup("Test2")?.First());
    }

    [Fact]
    public void Analyze_MethodRegisteredInBinder()
    {
        var (root, context) = ParseAndAnalyze();
        var symbolMap = context.AnalysisContext.Binders;

        var methodSymbol = symbolMap[root.Modules[0]].Lookup("MyMethod")?.First();
        Assert.IsType<MethodDefinitionSymbol>(methodSymbol);
        Assert.Equal(TypeSymbol.DefaultType.I32, ((MethodDefinitionSymbol)methodSymbol).ReturnType.KnownType);
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
        var caller = (MethodDeclarationNode)root.Modules[0].Body.Contents.First();
        var statement = Assert.IsType<ExpressionStatementNode>(caller.Body.Contents.First());
        var call = Assert.IsType<CallExpressionNode>(statement.Expression);
        Assert.Equal("MyMethod", call.MethodName.BaseName);
        Assert.Empty(call.Arguments);
    }

    [Fact]
    public void Parse_BinaryExpression_Addition()
    {
        var (root, _) = ParseAndAnalyze(ADD_EXPRESSION_SOURCE);
        var method = (MethodDeclarationNode)root.Modules[0].Body.Contents.First();
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
        var method = (MethodDeclarationNode)root.Modules[0].Body.Contents.First();
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
        var method = (MethodDeclarationNode)root.Modules[0].Body.Contents.First();
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

    [Fact]
    public void Analyze_ImportNode_BoundToModuleSymbol()
    {
        var (root, context) = ParseAndAnalyze(BINDING_SOURCE);
        var import = root.Imports[0];
        var symbol = context.AnalysisContext.ResolveSymbol(import.ModuleName).Symbol;
        Assert.IsType<ModuleSymbol>(symbol);
        Assert.Equal("Test2", ((ModuleSymbol)symbol).Name);
    }

    [Fact]
    public void Analyze_MethodDefinitionNode_BoundToMethodSymbol()
    {
        var (root, context) = ParseAndAnalyze(BINDING_SOURCE);
        var method = (MethodDeclarationNode)root.Modules[0].Body.Contents.First();
        var symbol = context.AnalysisContext.ResolveSymbol(method).Symbol;
        Assert.IsType<MethodDefinitionSymbol>(symbol);
        Assert.Equal("MyMethod", ((MethodDefinitionSymbol)symbol).Name);
    }

    [Fact]
    public void Analyze_ModuleNode_BoundToModuleSymbol()
    {
        var (root, context) = ParseAndAnalyze(BINDING_SOURCE);
        var module = root.Modules[0];
        var symbol = context.AnalysisContext.ResolveSymbol(module).Symbol;
        Assert.IsType<ModuleSymbol>(symbol);
        Assert.Equal("Test", ((ModuleSymbol)symbol).Name);
    }

    [Fact]
    public void Analyze_CallExpression_BoundToMethodSymbol()
    {
        var (root, context) = ParseAndAnalyze(BINDING_SOURCE);
        var method = (MethodDeclarationNode)root.Modules[0].Body.Contents.First();
        var returnStatement = (ReturnStatementNode)method.Body.Contents.First();
        var call = Assert.IsType<CallExpressionNode>(returnStatement.Expression);
        var symbol = context.AnalysisContext.ResolveSymbol(call.MethodName).Symbol;
        Assert.IsType<MethodDefinitionSymbol>(symbol);
        Assert.Equal("Helper", ((MethodDefinitionSymbol)symbol).Name);
    }

    [Fact]
    public void Parse_VariableDeclaration()
    {
        var (root, _) = ParseAndAnalyze(VARIABLE_DECLARATION_SOURCE);
        var method = (MethodDeclarationNode)root.Modules[0].Body.Contents.First();
        var decl = Assert.IsType<VariableDeclarationNode>(method.Body.Contents.First());
        Assert.Equal("x", decl.Name.BaseName);
        Assert.Equal("i32", decl.Type.Value);
        Assert.IsType<NumberLiteralNode>(decl.Initializer);
    }

    [Fact]
    public void Parse_VariableDeclaration_InitializerValue()
    {
        var (root, _) = ParseAndAnalyze(VARIABLE_DECLARATION_SOURCE);
        var method = (MethodDeclarationNode)root.Modules[0].Body.Contents.First();
        var decl = (VariableDeclarationNode)method.Body.Contents.First();
        var literal = Assert.IsType<NumberLiteralNode>(decl.Initializer);
        Assert.Equal("42", literal.Value);
    }

    [Fact]
    public void Analyze_VariableDeclaration_BoundToLocalVariableSymbol()
    {
        var (root, context) = ParseAndAnalyze(VARIABLE_DECLARATION_SOURCE);
        var method = (MethodDeclarationNode)root.Modules[0].Body.Contents.First();
        var decl = (VariableDeclarationNode)method.Body.Contents.First();
        var symbol = context.AnalysisContext.ResolveSymbol(decl).Symbol;
        Assert.IsType<LocalVariableSymbol>(symbol);
        Assert.Equal("x", ((LocalVariableSymbol)symbol).Name);
    }

    [Fact]
    public void Analyze_VariableDeclaration_HasCorrectType()
    {
        var (root, context) = ParseAndAnalyze(VARIABLE_DECLARATION_SOURCE);
        var method = (MethodDeclarationNode)root.Modules[0].Body.Contents.First();
        var decl = (VariableDeclarationNode)method.Body.Contents.First();
        var symbol = context.AnalysisContext.ResolveSymbol(decl).Symbol as LocalVariableSymbol;
        Assert.Equal(TypeSymbol.DefaultType.I32, symbol!.Type.KnownType);
    }

    [Fact]
    public void Analyze_VariableReference_BoundToLocalVariableSymbol()
    {
        var (root, context) = ParseAndAnalyze(VARIABLE_DECLARATION_SOURCE);
        var method = (MethodDeclarationNode)root.Modules[0].Body.Contents.First();
        var returnStatement = (ReturnStatementNode)method.Body.Contents.Last();
        var identifier = Assert.IsType<IdentifierNameNode>(returnStatement.Expression);
        var symbol = context.AnalysisContext.ResolveSymbol(identifier).Symbol;
        Assert.IsType<LocalVariableSymbol>(symbol);
        Assert.Equal("x", ((LocalVariableSymbol)symbol).Name);
    }

    [Fact]
    public void Analyze_VariableReference_HasCorrectType()
    {
        var (root, context) = ParseAndAnalyze(VARIABLE_DECLARATION_SOURCE);
        var method = (MethodDeclarationNode)root.Modules[0].Body.Contents.First();
        var returnStatement = (ReturnStatementNode)method.Body.Contents.Last();
        var identifier = Assert.IsType<IdentifierNameNode>(returnStatement.Expression);
        var type = context.AnalysisContext.ExpressionTypes[identifier];
        Assert.Equal(TypeSymbol.DefaultType.I32, type.KnownType);
    }

    [Fact]
    public void Analyze_VariableUsedInExpression_HasCorrectType()
    {
        var (root, context) = ParseAndAnalyze(VARIABLE_EXPRESSION_SOURCE);
        var method = (MethodDeclarationNode)root.Modules[0].Body.Contents.First();
        var secondDecl = (VariableDeclarationNode)method.Body.Contents[1];
        var binary = Assert.IsType<BinaryExpressionNode>(secondDecl.Initializer);
        var left = Assert.IsType<IdentifierNameNode>(binary.Left);
        var symbol = context.AnalysisContext.ResolveSymbol(left).Symbol;
        Assert.IsType<LocalVariableSymbol>(symbol);
        Assert.Equal("x", ((LocalVariableSymbol)symbol).Name);
    }
}