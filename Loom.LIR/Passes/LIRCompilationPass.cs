using Loom.Common;
using Loom.Common.Reflection;
using Loom.LIR.Objects;
using System.Reflection;

namespace Loom.LIR.Passes;

/// <summary>
/// Base class for all Loom Intermediate Representation passes that run on a <see cref="LIRCompilationUnit"/>.
/// </summary>
public abstract class LIRCompilationPass
{
    /// <summary> All registered compilation passes. </summary>
    public static List<LIRCompilationPass> Passes { get; } = LoomReflection.InstansiateAllWithAttribute<CompilationPassAttribute>(Assembly.GetExecutingAssembly()).Cast<LIRCompilationPass>().ToList();

    /// <summary> The compilation context the pass runs on. </summary>
    public CompilationContext? Context { get; set; }

    /// <summary> Runs the <see cref="LIRCompilationPass"/> on the specified <see cref="LIRCompilationUnit"/>. </summary>
    public abstract void Run(LIRCompilationUnit unit);
}