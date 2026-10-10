using Loom.Analyzer;
using Loom.Analyzer.Symbols;
using Loom.Common;
using Loom.Parser;

namespace Loom.Tests.Diagnostics;

/// <summary>
/// Tests that a call resolves to the one method among those sharing its name that accepts its arguments.
/// </summary>
public class OverloadResolutionTests
{
    public const string OVERLOADED_BY_PARAMETER_TYPE = @"
module Test
{
    struct Point
    {
        public i32 X = default;
    }

    export void print(char c) { }
    export void print(i32 value) { }
    export void print(Point p) { }

    export void Run()
    {
        print('a');
        print(1);
        print(new Point());
    }
}
";

    public const string OVERLOADED_BY_ARITY = @"
module Test
{
    export void log(i32 value) { }
    export void log(i32 value, i32 code) { }

    export void Run()
    {
        log(1, 2);
    }
}
";

    public const string NO_MATCHING_OVERLOAD = @"
module Test
{
    export void log(i32 value) { }
    export void log(i32 value, i32 code) { }

    export void Run()
    {
        log(1, 2, 3);
    }
}
";

    public const string AMBIGUOUS_OVERLOAD = @"
module Test
{
    export void log(i64 value) { }
    export void log(iptr value) { }

    export void Run()
    {
        log(1);
    }
}
";

    public const string OVERLOADED_MEMBER = @"
module Test
{
    struct Wrapper
    {
        public i32 Value = default;

        public i32 Get(char key) { return 0; }
        public i32 Get(i32 key) { return 0; }
    }

    export void Run()
    {
        Wrapper wrapper = new Wrapper();
        i32 fromChar = wrapper.Get('k');
        i32 fromNumber = wrapper.Get(1);
    }
}
";

    public const string GENERIC_AMONG_OVERLOADS = @"
module Test
{
    export void print(char c) { }
    export void print(i32 value) { }

    export T read<T>(T value) { return value; }

    export void Run()
    {
        print('a');

        i32 value = 1;
        print(read<i32>(value));
    }
}
";

    public const string EARLIER_ARGUMENT_DECIDES = @"
module Test
{
    export void log(i32 value, i64 code) { }
    export void log(i64 value, i32 code) { }

    export void Run()
    {
        log(1, 'c');
    }
}
";

    public const string EXTERN_METHOD = @"
module Test
{
    extern void putchar(char c);

    export void Run()
    {
        putchar('a');
    }
}
";

    public const string IMPORTING_MODULE = @"
module Standard::Memory
{
    export stack struct Slice
    {
        public i32 Length = default;
    }
}
";

    public const string IMPORTED_OVERLOADS = @"
import Standard::Memory;

module Standard::IO
{
    export void println(Slice s) { }
    export void println(char c) { }
}
";

    public const string IMPORTING_CALL_SITE = @"
import Standard::IO;
import Standard::Memory;

module Standard::Program
{
    export void Run()
    {
        Slice slice = default;
        println(slice);
        println('!');
    }
}
";

    [Fact]
    public void Analyze_OverloadedByParameterType_ResolvesEachCallToItsOwnOverload()
    {
        var context = ParseAndAnalyze(OVERLOADED_BY_PARAMETER_TYPE);

        Assert.Equal([".char", ".i32", ".Test::Point"], InvokedSignatures(context));
    }

    [Fact]
    public void Analyze_OverloadedByArity_ResolvesTheCallTakingThatManyArguments()
    {
        var context = ParseAndAnalyze(OVERLOADED_BY_ARITY);

        Assert.Equal([".i32_i32"], InvokedSignatures(context));
    }

    [Fact]
    public void Analyze_NoMatchingOverload_ReportsNoMatchingOverload()
    {
        Assert.Equal(["No overload of 'log' accepts 3 argument(s)."], OverloadDiagnostics(NO_MATCHING_OVERLOAD));
    }

    [Fact]
    public void Analyze_EquallyApplicableOverloads_ReportAmbiguity()
    {
        Assert.Equal(["The call to 'log' is ambiguous."], OverloadDiagnostics(AMBIGUOUS_OVERLOAD));
    }

    [Fact]
    public void Analyze_OverloadedMember_ResolvesByTheTypeOfTheReceiver()
    {
        var context = ParseAndAnalyze(OVERLOADED_MEMBER);

        Assert.Equal([".char", ".i32"], InvokedSignatures(context));
    }

    [Fact]
    public void Analyze_Overloads_AreCheckedAgainstTheOverloadTheyResolveTo()
    {
        var context = ParseAndAnalyze(GENERIC_AMONG_OVERLOADS);

        Assert.Equal([".char", ".i32", ".T"], InvokedSignatures(context));
        Assert.DoesNotContain(context.DiagnosticContext.Diagnostics, diagnostic => diagnostic.Message.Contains("implicitly cast"));
    }

    [Fact]
    public void Analyze_EquallyGoodMatchesInTotal_AreBrokenByTheFirstArgument()
    {
        var context = ParseAndAnalyze(EARLIER_ARGUMENT_DECIDES);

        Assert.Equal([".i32_i64"], InvokedSignatures(context));
    }

    [Fact]
    public void Analyze_Overloads_ShareAFullyQualifiedNameButNotALinkageName()
    {
        var overloads = ParseAndAnalyze(OVERLOADED_BY_ARITY).DeclaredMethods("log").ToArray();

        Assert.Equal(2, overloads.Length);
        Assert.All(overloads, overload => Assert.Equal("Test::log", overload.FullyQualifiedName));
        Assert.Equal(["Test::log.i32", "Test::log.i32_i32"], overloads.Select(overload => overload.LinkageName));
    }

    [Fact]
    public void Analyze_ExternMethod_KeepsTheNameItIsBoundTo()
    {
        var methods = ParseAndAnalyze(EXTERN_METHOD).DeclaredMethods("putchar");

        Assert.Equal(["putchar"], methods.Select(method => method.LinkageName));
    }

    [Fact]
    public void Analyze_ImportedOverloads_AreAllVisibleToTheImportingModule()
    {
        var context = ParseAndAnalyze(IMPORTING_MODULE, IMPORTED_OVERLOADS);

        Assert.Equal(["Standard::IO::println.Standard::Memory::Slice", "Standard::IO::println.char"], context.DeclaredMethods("println").Select(method => method.LinkageName));
    }

    [Fact]
    public void Analyze_ImportedOverloads_AreResolvedAtTheCallSite()
    {
        var context = ParseAndAnalyze(IMPORTING_MODULE, IMPORTED_OVERLOADS, IMPORTING_CALL_SITE);

        Assert.Equal([".Standard::Memory::Slice", ".char"], InvokedSignatures(context, "Standard::Program"));
    }

    private static CompilationContext ParseAndAnalyze(params string[] sources)
    {
        CompilationContext context = new CompilationContext();

        foreach (var source in sources)
            context.Parse(source);

        return context.Analyze();
    }

    private static List<string> OverloadDiagnostics(params string[] sources) => ParseAndAnalyze(sources).DiagnosticContext.Diagnostics
        .Where(diagnostic => diagnostic.Message.Contains("overload") || diagnostic.Message.Contains("ambiguous"))
        .Select(diagnostic => diagnostic.Message)
        .ToList();

    private static List<string> InvokedSignatures(CompilationContext context, string? module = null) => context.Calls(module)
        .Select(call => context.AnalysisContext.GetSymbol(call).As<MethodSymbol>()?.Signature ?? "<unresolved>")
        .ToList();
}
