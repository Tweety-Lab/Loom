using Loom.Analyzer;
using Loom.Common;
using Loom.LIR;
using Loom.LIR.OpCodes;
using Loom.Parser;

namespace Loom.Tests.Diagnostics;

public class StaticMemberAccessTests
{
    [Theory]
    [InlineData("Value", "Cannot access instance member 'Value' in a static function.")]
    [InlineData("Count", "Cannot access instance member 'Count' in a static function.")]
    [InlineData("Read()", "Cannot access instance member 'Read' in a static function.")]
    [InlineData("Identity(Value)", "Cannot access instance member 'Value' in a static function.")]
    public void StaticOperator_BareInstanceMember_ReportsMissingReceiver(string expression, params string[] expectedDiagnostics)
    {
        var context = Analyze($$"""
            module Test
            {
                class Buffer<T>
                {
                    i32 Value = 7;
                    public i32 Count { get; } = 3;
                    public i32 Read() { return Value; }
                    public static i32 Identity(i32 value) { return value; }
                    public static i32 operator[](Buffer<T> buffer, i32 index) { return {{expression}}; }
                }
            }
            """);

        Assert.Equal(expectedDiagnostics, context.DiagnosticContext.Diagnostics.Select(diagnostic => diagnostic.Message));
    }

    [Theory]
    [InlineData("buffer.Value")]
    [InlineData("buffer.Count")]
    [InlineData("buffer.Read()")]
    public void StaticOperator_ExplicitReceiver_EmitsBody(string expression)
    {
        var context = Analyze($$"""
            module Test
            {
                class Buffer<T>
                {
                    i32 Value = 7;
                    public i32 Count { get; } = 3;
                    public i32 Read() { return Value; }
                    public static i32 operator[](Buffer<T> buffer, i32 index) { return {{expression}}; }
                }
                export i32 ReadAt(ref Buffer<i32> buffer) { return buffer[0]; }
            }
            """);

        Assert.Empty(context.DiagnosticContext.Diagnostics);
        var unit = Assert.Single(context.EmitLIR().CompilationUnits);
        var op = Assert.Single(unit.AllFunctions, function => function.Name.Contains("Buffer<i32>::.[]"));
        Assert.Equal(new[] { "buffer", "index" }, op.Type.Parameters.Select(parameter => parameter.Name));
        Assert.Contains(op.Blocks.SelectMany(block => block.Instructions), instruction => instruction.OpCode == LIROpCode.Return);
    }

    [Theory]
    [InlineData("return Value;")]
    [InlineData("Value = 8; return 0;")]
    [InlineData("Value++; return 0;")]
    public void StaticMethod_BareInstanceField_ReportsMissingReceiver(string body)
    {
        var context = Analyze($$"""
            module Test
            {
                class Buffer { i32 Value = 7; public static i32 Read() { {{body}} } }
            }
            """);
        Assert.Equal("Cannot access instance member 'Value' in a static function.", Assert.Single(context.DiagnosticContext.Diagnostics).Message);
    }

    [Fact]
    public void StaticOperator_ShadowedFieldAndStaticCall_AreAllowed()
    {
        var context = Analyze("""
            module Test
            {
                struct Buffer
                {
                    i32 Value = 7;
                    public static i32 Read() { return 3; }
                    public static i32 operator[](Buffer buffer, i32 Value) { return Value + Read(); }
                }
            }
            """);
        Assert.Empty(context.DiagnosticContext.Diagnostics);
        context.EmitLIR();
    }

    [Theory]
    [InlineData("1")]
    [InlineData("true", "Cannot access instance member 'Read' in a static function.")]
    public void StaticMethod_OverloadedCall_ChecksSelectedMethod(string argument, params string[] expectedDiagnostics)
    {
        var context = Analyze($$"""
            module Test
            {
                struct Buffer
                {
                    public static i32 Read(i32 value) { return value; }
                    public i32 Read(bool value) { return 3; }
                    public static i32 Run() { return Read({{argument}}); }
                }
            }
            """);

        Assert.Equal(expectedDiagnostics, context.DiagnosticContext.Diagnostics.Select(diagnostic => diagnostic.Message));
    }

    private static CompilationContext Analyze(string source) => new CompilationContext().Parse(source).Analyze();
}
