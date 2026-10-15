using Loom.Analyzer;
using Loom.Common;
using Loom.Parser;

namespace Loom.Tests.Diagnostics;

public class StaticTypeTests
{
    public const string STATIC_CLASS_IS_NOT_INSTANTIABLE = @"
module Test
{
    export static class Utility
    {
        public static i32 Value() { return 0; }
    }

    export void Run()
    {
        Utility utility = new Utility();
    }
}
";

    public const string STATIC_CLASS_WITH_INSTANCE_FIELD = @"
module Test
{
    export static class Utility
    {
        public i32 Value = default;
    }
}
";

    public const string STATIC_CLASS_WITH_INSTANCE_METHOD = @"
module Test
{
    export static class Utility
    {
        public i32 Value() { return 0; }
    }
}
";

    public const string STATIC_CLASS_WITH_INSTANCE_PROPERTY = @"
module Test
{
    export static class Utility
    {
        public i32 Value { get; set; } = default;
    }
}
";

    public const string STATIC_CLASS_WITH_STATIC_MEMBERS = @"
module Test
{
    export static class Utility
    {
        public static i32 Value() { return 0; }

        public static i32 Count { get; set; } = default;
    }
}
";

    public const string INSTANCE_CLASS_WITH_INSTANCE_MEMBERS = @"
module Test
{
    export class Box
    {
        public i32 Value = default;

        public i32 Read() { return Value; }

        public i32 Number { get; set; } = default;
    }
}
";

    public const string STATIC_STRUCT_IS_NOT_INSTANTIABLE = @"
module Test
{
    export static struct Marker
    {
    }

    export void Run()
    {
        Marker marker = new Marker();
    }
}
";

    [Fact]
    public void Analyze_InstantiatingAStaticType_ReportsCannotInstantiate() => Assert.Equal(["Cannot instantiate static type 'Utility'."], StaticTypeDiagnostics(STATIC_CLASS_IS_NOT_INSTANTIABLE));

    [Fact]
    public void Analyze_InstanceFieldInStaticType_ReportsInstanceMember() => Assert.Equal(["Cannot declare instance member 'Value' in static type 'Utility'."], StaticTypeDiagnostics(STATIC_CLASS_WITH_INSTANCE_FIELD));

    [Fact]
    public void Analyze_InstanceMethodInStaticType_ReportsInstanceMember() => Assert.Equal(["Cannot declare instance member 'Value' in static type 'Utility'."], StaticTypeDiagnostics(STATIC_CLASS_WITH_INSTANCE_METHOD));

    [Fact]
    public void Analyze_InstancePropertyInStaticType_ReportsInstanceMember() => Assert.Equal(["Cannot declare instance member 'Value' in static type 'Utility'."], StaticTypeDiagnostics(STATIC_CLASS_WITH_INSTANCE_PROPERTY));

    [Fact]
    public void Analyze_StaticMembersOfStaticType_AreAllowed() => Assert.Empty(StaticTypeDiagnostics(STATIC_CLASS_WITH_STATIC_MEMBERS));

    [Fact]
    public void Analyze_InstanceMembersOfInstanceType_AreAllowed() => Assert.Empty(StaticTypeDiagnostics(INSTANCE_CLASS_WITH_INSTANCE_MEMBERS));

    [Fact]
    public void Analyze_InstantiatingAStaticStruct_ReportsCannotInstantiate() => Assert.Equal(["Cannot instantiate static type 'Marker'."], StaticTypeDiagnostics(STATIC_STRUCT_IS_NOT_INSTANTIABLE));

    private static CompilationContext ParseAndAnalyze(string source)
    {
        CompilationContext context = new CompilationContext();
        context.Parse(source).Analyze();
        return context;
    }

    private static List<string> StaticTypeDiagnostics(string source) => ParseAndAnalyze(source).DiagnosticContext.Diagnostics
        .Where(diagnostic => diagnostic.Message.Contains("static type"))
        .Select(diagnostic => diagnostic.Message)
        .ToList();
}
