using Loom.Analyzer;
using Loom.Analyzer.Symbols;
using Loom.Common;
using Loom.Parser;
using Loom.Parser.AST.Rules.Default;

namespace Loom.Tests.Diagnostics;

/// <summary>
/// Tests unique ownership semantics.
/// </summary>
public class OwnershipTests
{
    public const string MOVE_OUT_VIA_LOCAL_DECLARATION = @"
module Test
{
    class Handle
    {
        public i32 Value = 0;
    }

    void MyMethod()
    {
        unique Handle first = new Handle();
        unique Handle second = first;
        unique Handle third = first;
    }
}
";

    public const string MOVE_OUT_VIA_OWNED_PARAMETER = @"
module Test
{
    class Handle
    {
        public i32 Value = 0;
    }

    void Consume(Handle h)
    {
    }

    void MyMethod()
    {
        unique Handle a = new Handle();
        Consume(a);
        Consume(a);
    }
}
";

    public const string MOVE_OUT_VIA_RETURN = @"
module Test
{
    class Handle
    {
        public i32 Value = 0;
    }

    unique Handle MyMethod()
    {
        unique Handle a = new Handle();
        unique Handle b = a;
        return a;
    }
}
";

    public const string MOVE_OUT_VIA_ASSIGNMENT = @"
module Test
{
    class Handle
    {
        public i32 Value = 0;
    }

    void MyMethod()
    {
        unique Handle a = new Handle();
        unique Handle b = new Handle();
        b = a;
        unique Handle c = a;
    }
}
";

    public const string BORROW_VIA_REF_PARAMETER = @"
module Test
{
    class Handle
    {
        public i32 Value = 0;
    }

    void Observe(ref Handle h)
    {
    }

    void MyMethod()
    {
        unique Handle a = new Handle();
        Observe(a);
        Observe(a);
    }
}
";

    public const string BORROW_VIA_MUT_REF_PARAMETER = @"
module Test
{
    class Handle
    {
        public i32 Value = 0;
    }

    void Touch(ref mut Handle h)
    {
        h.Value = 1;
    }

    void MyMethod()
    {
        unique Handle a = new Handle();
        Touch(a);
        Touch(a);
    }
}
";

    public const string REASSIGN_RESTORES_OWNERSHIP = @"
module Test
{
    class Handle
    {
        public i32 Value = 0;
    }

    void Consume(Handle h)
    {
    }

    void MyMethod()
    {
        unique Handle a = new Handle();
        Consume(a);
        a = new Handle();
        Consume(a);
    }
}
";

    public const string COPY_VALUE_MAY_BE_REUSED = @"
module Test
{
    void MyMethod()
    {
        i32 x = 1;
        i32 y = x;
        i32 z = x;
    }
}
";

    public const string MEMBER_READ_AFTER_MOVE = @"
module Test
{
    class Handle
    {
        public i32 Value = 0;
    }

    void Consume(Handle h)
    {
    }

    void MyMethod()
    {
        unique Handle a = new Handle();
        Consume(a);
        i32 v = a.Value;
    }
}
";

    public const string MEMBER_WRITE_AFTER_MOVE = @"
module Test
{
    class Handle
    {
        public i32 Value = 0;
    }

    void Consume(Handle h)
    {
    }

    void MyMethod()
    {
        unique Handle a = new Handle();
        Consume(a);
        a.Value = 3;
    }
}
";

    public const string MEMBER_WRITE_VIA_BORROW_DOES_NOT_MOVE = @"
module Test
{
    class Handle
    {
        public i32 Value = 0;
    }

    void Touch(ref mut Handle h)
    {
        h.Value = 1;
    }

    void Consume(Handle h)
    {
    }

    void MyMethod()
    {
        unique Handle a = new Handle();
        Touch(a);
        Consume(a);
    }
}
";

    public const string DISTINCT_LOCALS_ARE_INDEPENDENT = @"
module Test
{
    class Handle
    {
        public i32 Value = 0;
    }

    void Consume(Handle h)
    {
    }

    void MyMethod()
    {
        unique Handle a = new Handle();
        unique Handle b = new Handle();
        Consume(a);
        Consume(b);
    }
}
";

    public const string USE_AFTER_MOVE_NAMES_ONLY_MOVED_LOCAL = @"
module Test
{
    class Handle
    {
        public i32 Value = 0;
    }

    void Consume(Handle h)
    {
    }

    void MyMethod()
    {
        unique Handle a = new Handle();
        unique Handle b = new Handle();
        Consume(a);
        Consume(b);
        Consume(a);
    }
}
";

    public const string MOVE_STATE_IS_PER_METHOD = @"
module Test
{
    class Handle
    {
        public i32 Value = 0;
    }

    void Consume(Handle h)
    {
    }

    void First()
    {
        unique Handle a = new Handle();
        Consume(a);
    }

    void Second()
    {
        unique Handle a = new Handle();
        Consume(a);
    }
}
";

    public const string BORROW_AFTER_MOVE = @"
module Test
{
    class Handle
    {
        public i32 Value = 0;
    }

    void Consume(Handle h)
    {
    }

    void Observe(ref Handle h)
    {
    }

    void MyMethod()
    {
        unique Handle a = new Handle();
        Consume(a);
        Observe(a);
    }
}
";

    [Fact]
    public void Analyze_UniqueLocal_IsBoundToAnOwningSymbol()
    {
        var context = ParseAndAnalyze(MOVE_OUT_VIA_LOCAL_DECLARATION);
        var declaration = UniqueLocalDeclaration(context);

        var symbol = context.AnalysisContext.GetSymbol(declaration).Symbol as LocalVariableSymbol;

        Assert.NotNull(symbol);
        Assert.Equal(PointerType.Unique, symbol.PointerType);
    }

    [Theory]
    [InlineData(MOVE_OUT_VIA_LOCAL_DECLARATION, "Cannot use moved value 'first'.")]
    [InlineData(MOVE_OUT_VIA_OWNED_PARAMETER, "Cannot use moved value 'a'.")]
    [InlineData(MOVE_OUT_VIA_RETURN, "Cannot use moved value 'a'.")]
    [InlineData(MOVE_OUT_VIA_ASSIGNMENT, "Cannot use moved value 'a'.")]
    [InlineData(BORROW_VIA_REF_PARAMETER)]
    [InlineData(BORROW_VIA_MUT_REF_PARAMETER)]
    [InlineData(REASSIGN_RESTORES_OWNERSHIP)]
    [InlineData(COPY_VALUE_MAY_BE_REUSED)]
    [InlineData(MEMBER_READ_AFTER_MOVE, "Cannot use moved value 'a'.")]
    [InlineData(MEMBER_WRITE_AFTER_MOVE, "Cannot use moved value 'a'.")]
    [InlineData(MEMBER_WRITE_VIA_BORROW_DOES_NOT_MOVE)]
    [InlineData(DISTINCT_LOCALS_ARE_INDEPENDENT)]
    [InlineData(USE_AFTER_MOVE_NAMES_ONLY_MOVED_LOCAL, "Cannot use moved value 'a'.")]
    [InlineData(MOVE_STATE_IS_PER_METHOD)]
    [InlineData(BORROW_AFTER_MOVE, "Cannot use moved value 'a'.")]
    public void Analyze_OwnershipRules_ReportExpectedMoveDiagnostics(string source, params string[] expectedDiagnostics) => Assert.Equal(expectedDiagnostics, MoveDiagnostics(source));

    private static CompilationContext ParseAndAnalyze(string source)
    {
        CompilationContext context = new CompilationContext();
        context.Parse(source).Analyze();
        return context;
    }

    private static List<string> MoveDiagnostics(string source) => ParseAndAnalyze(source).DiagnosticContext.Diagnostics.Where(d => d.Message.Contains("moved value")).Select(d => d.Message).ToList();

    [Theory]
    [InlineData("public unique Handle Get() { return Value; }", "Cannot transfer ownership of member 'Value' because member access borrows its value.")]
    [InlineData("public unique Handle Get() { return Field; }", "Cannot transfer ownership of member 'Field' because member access borrows its value.")]
    [InlineData("public unique Handle Get(ref Holder other) { return other.Value; }", "Cannot transfer ownership of member 'Value' because member access borrows its value.")]
    [InlineData("public unique Handle Get(ref IHolder other) { return other.Value; }", "Cannot transfer ownership of member 'Value' because member access borrows its value.")]
    [InlineData("public unique Handle Get(ref Handle value) { return value; }", "Cannot transfer ownership of borrowed parameter 'value'.")]
    [InlineData("public unique Handle Get(ref mut Handle value) { return value; }", "Cannot transfer ownership of borrowed parameter 'value'.")]
    [InlineData("public unique Handle Get() { return new Handle(); }")]
    [InlineData("public unique Handle Get() { unique Handle value = new Handle(); return value; }")]
    [InlineData("public i32 Get() { return Number; }")]
    public void Analyze_PublicMethodReturn_RequiresMovableValue(string method, params string[] expectedDiagnostics)
    {
        var source = $$"""
            module Test
            {
                class Handle { }
                interface IHolder
                {
                    unique Handle Value { get; set; }
                }
                class Holder : IHolder
                {
                    public unique Handle Value { get; set; }
                    Handle Field = default;
                    i32 Number = 1;
                    {{method}}
                }
            }
            """;

        Assert.Equal(expectedDiagnostics, ParseAndAnalyze(source).DiagnosticContext.Diagnostics.Select(d => d.Message));
    }

    [Theory]
    [InlineData("class Holder { public Handle Value { get; set; } }", "Reference type 'Handle' must declare a pointer type.")]
    [InlineData("interface Holder { Handle Value { get; set; } }", "Reference type 'Handle' must declare a pointer type.")]
    [InlineData("class Holder { public static Handle Value { get; } }", "Reference type 'Handle' must declare a pointer type.")]
    [InlineData("class Holder { public unique Handle Value { get; set; } }")]
    [InlineData("interface Holder { unique Handle Value { get; set; } }")]
    [InlineData("class Holder { private Handle Value { get; set; } }")]
    [InlineData("class Holder { public i32 Value { get; set; } }")]
    [InlineData("class Holder { public Data Value { get; set; } }")]
    [InlineData("class Holder<T> { public T Value { get; set; } }")]
    [InlineData("class Holder<T> { public Box<T> Value { get; set; } }", "Reference type 'Box' must declare a pointer type.")]
    [InlineData("class Holder<T> { public unique Box<T> Value { get; set; } }")]
    public void Analyze_PublicReferenceProperty_RequiresPointerType(string declaration, params string[] expectedDiagnostics)
    {
        var source = $$"""
            module Test
            {
                class Handle { }
                struct Data { }
                class Box<T> { }
                {{declaration}}
            }
            """;

        Assert.Equal(expectedDiagnostics, ParseAndAnalyze(source).DiagnosticContext.Diagnostics.Select(d => d.Message));
    }

    private static LocalDeclarationStatementNode UniqueLocalDeclaration(CompilationContext context)
    {
        var module = context.SyntaxTrees.First().Modules.First();
        var method = (MethodDeclarationNode)module.Body.Contents.First(n => n is MethodDeclarationNode { Name.Text: "MyMethod" });
        return (LocalDeclarationStatementNode)method.Body!.Contents.First();
    }
}
