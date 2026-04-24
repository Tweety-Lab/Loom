
namespace Loom.Analyzer.Analyzers;

/// <summary>
/// Registers a <see cref="Analyzer"/> into the analyzer pipeline.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class LoomAnalyzerAttribute : Attribute { }
