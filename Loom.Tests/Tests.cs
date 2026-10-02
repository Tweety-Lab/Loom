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
using Loom.Parser.Tokenizer;
using Loom.Parser.AST.Rules.Default;

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

    public const string MEMBER_CALL_SOURCE = @"
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
        i32 result = obj.Number();
    }
}
";

    public const string INVALID_STATEMENT_MODULE_SOURCE = @"
module Test
{
    i32 x = 42;
}
";

    public const string INVALID_STATEMENT_STRUCT_SOURCE = @"
module Test
{
    struct TestStruct
    {
        i32 x = 42;
    }
}
";

    public const string INVALID_STATEMENT_CONDITIONAL_SOURCE = @"
module Test
{
    if (true)
    {
        return 1;
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
        Assert.Equal("i32", decl.Type.Base.Text);
        Assert.IsType<NumberLiteralNode>(decl.Initializer);
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
        var conditional = Assert.IsType<IfStatementNode>(method.Body.Contents[1]);

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
        var symbol = context.AnalysisContext.GetSymbol(method).Symbol as MethodSymbol;
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

        var creation = Assert.IsType<InstanceCreationExpresssionNode>(decl.Initializer);
        Assert.Equal("TestStruct", creation.TypeName.BaseName);
    }

    [Fact]
    public void Analyze_ObjectCreationExpression_HasStructType()
    {
        var (root, context) = ParseAndAnalyze(OBJECT_CREATION_SOURCE);
        var method = (MethodDeclarationNode)root.Modules[0].Body.Contents[1];
        var decl = (VariableDeclarationNode)method.Body.Contents.First();
        var creation = Assert.IsType<InstanceCreationExpresssionNode>(decl.Initializer);

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
    public void EmitLIR_ObjectCreationExpression_ConstructsInPlaceAndCallsConstructor()
    {
        CompilationContext context = new CompilationContext();
        context.Parse(OBJECT_CREATION_SOURCE).Analyze().EmitLIR();

        LIRCompilationUnit unit = context.CompilationUnits.First();
        var main = unit.GetFunction("Test::MyMethod");
        Assert.NotNull(main);

        var instructions = main!.Blocks.SelectMany(b => b.Instructions).ToList();

        // A value type is constructed into the storage of the variable itself, so it is only allocated once.
        var alloca = Assert.Single(instructions, i => i.OpCode == LIROpCode.Alloca);
        Assert.IsType<LIRTypeDeclarationType>(alloca.Result!.Type);

        Assert.DoesNotContain(instructions, i => i.OpCode == LIROpCode.Store);

        var call = Assert.Single(instructions, i => i.OpCode == LIROpCode.CallInstanced);
        Assert.Equal("Test::TestStruct::.ctor", Assert.IsType<LIRFunction>(call.Operands[0]).Name);
        Assert.Same(alloca.Result, call.Operands[1]);
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

    [Fact]
    public void EmitLIR_MemberCall_EmitsCallInstancedWithReceiver()
    {
        CompilationContext context = new CompilationContext();
        context.Parse(MEMBER_CALL_SOURCE).Analyze().EmitLIR();

        LIRCompilationUnit unit = context.CompilationUnits.First();
        var main = unit.GetFunction("Test::MyMethod");
        Assert.NotNull(main);

        var instanced = main!.Blocks.SelectMany(b => b.Instructions).First(i => i.OpCode == LIROpCode.CallInstanced);
        Assert.Equal(2, instanced.Operands.Count);

        var target = Assert.IsType<LIRFunction>(instanced.Operands[0]);
        Assert.Equal("Test::TestStruct::Number", target.Name);

        var selfParam = target.Type.Parameters.First();
        Assert.Equal("self", selfParam.Name);
        Assert.IsType<LIRPointerType>(selfParam.Type);
        Assert.Equal(LIRType.Int32, target.Type.ReturnType);
    }

    [Fact]
    public void TranslateLLVM_MemberCall_PassesReceiverToInstanceMethod()
    {
        CompilationContext context = new CompilationContext();
        context.Parse(MEMBER_CALL_SOURCE).Analyze();

        LIRCompilationUnit unit = context.EmitLIR().CompilationUnits.First();

        LLVMTranslatorPass translator = new LLVMTranslatorPass();
        translator.Run(unit);

        string llvmIr = translator.Result.PrintToString();
        Assert.Contains("define i32 @\"Test::TestStruct::Number\"(ptr %self)", llvmIr);
        Assert.Contains("call i32 @\"Test::TestStruct::Number\"(ptr", llvmIr);
    }

    [Fact]
    public void Analyze_StatementInModuleBody_ReportsDiagnostic()
    {
        var (_, context) = ParseAndAnalyze(INVALID_STATEMENT_MODULE_SOURCE);
        var diagnostic = Assert.Single(context.DiagnosticContext.Diagnostics, d => d.Level == Diagnostic.DiagnosticLevel.Error);
        Assert.Contains("local variable declaration", diagnostic.Message);
        Assert.Contains("module 'Test'", diagnostic.Message);
    }

    [Fact]
    public void Analyze_StatementInStructBody_ReportsDiagnostic()
    {
        var (_, context) = ParseAndAnalyze(INVALID_STATEMENT_STRUCT_SOURCE);
        var diagnostic = Assert.Single(context.DiagnosticContext.Diagnostics, d => d.Level == Diagnostic.DiagnosticLevel.Error);
        Assert.Contains("struct 'TestStruct'", diagnostic.Message);
    }

    [Fact]
    public void Analyze_MisplacedConditional_ReportsOnceForNestedStatements()
    {
        var (_, context) = ParseAndAnalyze(INVALID_STATEMENT_CONDITIONAL_SOURCE);
        var diagnostic = Assert.Single(context.DiagnosticContext.Diagnostics, d => d.Level == Diagnostic.DiagnosticLevel.Error);
        Assert.Contains("if statement", diagnostic.Message);
    }
}