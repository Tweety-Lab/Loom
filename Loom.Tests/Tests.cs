using Loom.Analyzer;
using Loom.Analyzer.Symbols;
using Loom.CodeGen.LLVM;
using Loom.Common;
using Loom.Common.Diagnostics;
using Loom.LIR;
using Loom.LIR.OpCodes;
using Loom.LIR.Objects;
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

    public const string EQUALITY_EXPRESSION_SOURCE = @"
module Test
{
    bool MyMethod()
    {
        return 1 == 2;
    }
}
";

    public const string RELATIONAL_EXPRESSION_SOURCE = @"
module Test
{
    bool MyMethod()
    {
        return 1 < 2;
    }
}
";

    public const string CONDITIONAL_COMPARISON_SOURCE = @"
module Test
{
    i32 MyMethod()
    {
        i32 x = 1;
        if (x == 1)
        {
            return 10;
        }

        return 0;
    }
}
";

    public const string IPTR_RETURN_SOURCE = @"
module Test
{
    export iptr MyMethod()
    {
        return 0;
    }
}
";

    public const string IPTR_USAGE_SOURCE = @"
module Test
{
    export iptr MyMethod()
    {
        return GetPointer(100);
    }

    export extern iptr GetPointer(i32 size);
}
";

    public const string OBJECT_CREATION_SOURCE = @"
module Test
{
    struct TestStruct
    {
        i32 Number()
        {
            return 1;
        }
    }

    void MyMethod()
    {
        TestStruct obj = new TestStruct();
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
        Assert.Equal("Test", root.Modules[0].Name.Text);
        Assert.Equal("Test2", root.Modules[1].Name.Text);
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
        var symbol = context.AnalysisContext.GetSymbol(import.ModuleName).Symbol;
        Assert.IsType<ModuleSymbol>(symbol);
        Assert.Equal("Test2", ((ModuleSymbol)symbol).Name);
    }

    [Fact]
    public void Analyze_MethodDefinitionNode_BoundToMethodSymbol()
    {
        var (root, context) = ParseAndAnalyze(BINDING_SOURCE);
        var method = (MethodDeclarationNode)root.Modules[0].Body.Contents.First();
        var symbol = context.AnalysisContext.GetSymbol(method).Symbol;
        Assert.IsType<MethodDefinitionSymbol>(symbol);
        Assert.Equal("MyMethod", ((MethodDefinitionSymbol)symbol).Name);
    }

    [Fact]
    public void Analyze_ModuleNode_BoundToModuleSymbol()
    {
        var (root, context) = ParseAndAnalyze(BINDING_SOURCE);
        var module = root.Modules[0];
        var symbol = context.AnalysisContext.GetSymbol(module).Symbol;
        Assert.IsType<ModuleSymbol>(symbol);
        Assert.Equal("Test", ((ModuleSymbol)symbol).Name);
    }

    [Fact]
    public void Parse_VariableDeclaration()
    {
        var (root, _) = ParseAndAnalyze(VARIABLE_DECLARATION_SOURCE);
        var method = (MethodDeclarationNode)root.Modules[0].Body.Contents.First();
        var decl = Assert.IsType<VariableDeclarationNode>(method.Body.Contents.First());
        Assert.Equal("x", decl.Name.Text);
        Assert.Equal("i32", decl.Type.Text);
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
        var symbol = context.AnalysisContext.GetSymbol(decl).Symbol;
        Assert.IsType<LocalVariableSymbol>(symbol);
        Assert.Equal("x", ((LocalVariableSymbol)symbol).Name);
    }

    [Fact]
    public void Analyze_VariableDeclaration_HasCorrectType()
    {
        var (root, context) = ParseAndAnalyze(VARIABLE_DECLARATION_SOURCE);
        var method = (MethodDeclarationNode)root.Modules[0].Body.Contents.First();
        var decl = (VariableDeclarationNode)method.Body.Contents.First();
        var symbol = context.AnalysisContext.GetSymbol(decl).Symbol as LocalVariableSymbol;
        Assert.Equal(TypeSymbol.DefaultType.I32, symbol!.Type.KnownType);
    }

    [Fact]
    public void Analyze_VariableReference_BoundToLocalVariableSymbol()
    {
        var (root, context) = ParseAndAnalyze(VARIABLE_DECLARATION_SOURCE);
        var method = (MethodDeclarationNode)root.Modules[0].Body.Contents.First();
        var returnStatement = (ReturnStatementNode)method.Body.Contents.Last();
        var identifier = Assert.IsType<IdentifierNameNode>(returnStatement.Expression);
        var symbol = context.AnalysisContext.GetSymbol(identifier).Symbol;
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
        var symbol = context.AnalysisContext.GetSymbol(left).Symbol;
        Assert.IsType<LocalVariableSymbol>(symbol);
        Assert.Equal("x", ((LocalVariableSymbol)symbol).Name);
    }

    [Fact]
    public void Parse_BinaryExpression_Equality()
    {
        var (root, _) = ParseAndAnalyze(EQUALITY_EXPRESSION_SOURCE);
        var method = (MethodDeclarationNode)root.Modules[0].Body.Contents.First();
        var returnStatement = (ReturnStatementNode)method.Body.Contents.First();

        var binary = Assert.IsType<BinaryExpressionNode>(returnStatement.Expression);
        Assert.Equal(Token.TokenType.EqualEqual, binary.Operator.Type);
    }

    [Fact]
    public void Parse_BinaryExpression_Relational()
    {
        var (root, _) = ParseAndAnalyze(RELATIONAL_EXPRESSION_SOURCE);
        var method = (MethodDeclarationNode)root.Modules[0].Body.Contents.First();
        var returnStatement = (ReturnStatementNode)method.Body.Contents.First();

        var binary = Assert.IsType<BinaryExpressionNode>(returnStatement.Expression);
        Assert.Equal(Token.TokenType.Less, binary.Operator.Type);
    }

    [Fact]
    public void Parse_Conditional_WithComparison()
    {
        var (root, _) = ParseAndAnalyze(CONDITIONAL_COMPARISON_SOURCE);
        var method = (MethodDeclarationNode)root.Modules[0].Body.Contents.First();
        var conditional = Assert.IsType<ConditionalNode>(method.Body.Contents[1]);

        var binary = Assert.IsType<BinaryExpressionNode>(conditional.Expression);
        Assert.Equal(Token.TokenType.EqualEqual, binary.Operator.Type);
    }

    [Fact]
    public void Analyze_Conditional_WithComparison_DoesNotThrow()
    {
        var (_, context) = ParseAndAnalyze(CONDITIONAL_COMPARISON_SOURCE);
        Assert.DoesNotContain(context.DiagnosticContext.Diagnostics, d => d.Level == Diagnostic.DiagnosticLevel.Error);
    }

    [Fact]
    public void Analyze_IPtrMethodBoundToIPtrType()
    {
        var (root, context) = ParseAndAnalyze(IPTR_RETURN_SOURCE);
        var method = (MethodDeclarationNode)root.Modules[0].Body.Contents.First();
        var symbol = context.AnalysisContext.GetSymbol(method).Symbol as MethodDefinitionSymbol;
        Assert.Equal(TypeSymbol.DefaultType.IPtr, symbol!.ReturnType.KnownType);
        Assert.Equal("iptr", symbol.ReturnType.Name);
    }

    [Fact]
    public void Analyze_ReturningI32LiteralFromIPtrMethod_ReportsTypeMismatch()
    {
        var (_, context) = ParseAndAnalyze(IPTR_RETURN_SOURCE);
        Assert.Contains(context.DiagnosticContext.Diagnostics, d => d.Level == Diagnostic.DiagnosticLevel.Error);
    }

    [Fact]
    public void Analyze_IPtrValueFlowThroughCall_NoDiagnostics()
    {
        var (_, context) = ParseAndAnalyze(IPTR_USAGE_SOURCE);
        Assert.DoesNotContain(context.DiagnosticContext.Diagnostics, d => d.Level == Diagnostic.DiagnosticLevel.Error);
    }

    [Fact]
    public void Parse_ObjectCreationExpression()
    {
        var (root, _) = ParseAndAnalyze(OBJECT_CREATION_SOURCE);
        var method = (MethodDeclarationNode)root.Modules[0].Body.Contents[1];
        var decl = Assert.IsType<VariableDeclarationNode>(method.Body.Contents.First());

        var creation = Assert.IsType<ObjectCreationExpressionNode>(decl.Initializer);
        Assert.Equal("TestStruct", creation.ObjectName.BaseName);
    }

    [Fact]
    public void Analyze_ObjectCreationExpression_HasStructType()
    {
        var (root, context) = ParseAndAnalyze(OBJECT_CREATION_SOURCE);
        var method = (MethodDeclarationNode)root.Modules[0].Body.Contents[1];
        var decl = (VariableDeclarationNode)method.Body.Contents.First();
        var creation = Assert.IsType<ObjectCreationExpressionNode>(decl.Initializer);

        var type = context.AnalysisContext.ExpressionTypes[creation];
        Assert.Equal(TypeSymbol.DefaultType.Struct, type.KnownType);
        Assert.Equal("TestStruct", type.Name);
    }

    [Fact]
    public void Analyze_ObjectCreationExpression_NoDiagnostics()
    {
        var (_, context) = ParseAndAnalyze(OBJECT_CREATION_SOURCE);
        Assert.DoesNotContain(context.DiagnosticContext.Diagnostics, d => d.Level == Diagnostic.DiagnosticLevel.Error);
    }

    [Fact]
    public void EmitLIR_ObjectCreationExpression_EmitsAllocaAndStore()
    {
        CompilationContext context = new CompilationContext();
        context.Parse(OBJECT_CREATION_SOURCE).Analyze().EmitLIR();

        LIRCompilationUnit unit = context.CompilationUnits.First();
        var main = unit.GetFunction("Test::MyMethod");
        Assert.NotNull(main);

        var instructions = main!.Blocks.SelectMany(b => b.Instructions).ToList();
        Assert.Contains(instructions, i => i.OpCode == LIROpCode.Alloca);
        Assert.Contains(instructions, i => i.OpCode == LIROpCode.Store);
        Assert.DoesNotContain(instructions, i => i.OpCode == LIROpCode.Call);
    }

    [Fact]
    public void TranslateLLVM_ObjectCreationExpression_ProducesModule()
    {
        CompilationContext context = new CompilationContext();
        context.Parse(OBJECT_CREATION_SOURCE).Analyze();

        LIRCompilationUnit unit = context.EmitLIR().CompilationUnits.First();

        LLVMTranslatorPass translator = new LLVMTranslatorPass();
        translator.Run(unit);

        string llvmIr = translator.Result.PrintToString();
        Assert.Contains("Test::MyMethod", llvmIr);
    }
}