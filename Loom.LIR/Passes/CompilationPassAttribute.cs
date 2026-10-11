namespace Loom.LIR.Passes;

/// <summary>
/// Registers a <see cref="LIRCompilationPass"/> into the Loom Intermediate Representation compilation pipeline.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class CompilationPassAttribute : Attribute { }