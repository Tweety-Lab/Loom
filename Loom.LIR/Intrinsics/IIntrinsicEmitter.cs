
using Loom.Analyzer.Symbols;
using Loom.Common.Reflection;
using Loom.LIR.Objects;
using System.Reflection;

namespace Loom.LIR.Intrinsics;

/// <summary>
/// Marks a class as an intrinsic emitter.
/// </summary>
/// <remarks>
/// An intrinsic emitter is a class that can be used to programmatically fill in the body of a specific function.
/// </remarks>
internal interface IIntrinsicEmitter
{ 
    /// <summary> All registered intrinsic emitters. </summary>
    public static List<IIntrinsicEmitter> Emitters { get; } = LoomReflection.InstansiateAllWithAttribute<IntrinsicEmitterAttribute>(Assembly.GetExecutingAssembly()).Cast<IIntrinsicEmitter>().ToList();

    /// <summary> Emit the intrinsic into the body of the given function. </summary>
    void EmitInto(LIRFunction function);

    /// <summary> Checks if the given method has an intrinsic emitter. </summary>
    public static bool HasIntrinsicEmitter(MethodSymbol method) => Emitters.Any(emitter => emitter.GetType().GetCustomAttribute<IntrinsicEmitterAttribute>()?.FullyQualifiedName == method.FullyQualifiedName);

    /// <summary> Emits the intrinsic into the body of the given function. </summary>
    /// <returns> <see langword="true"/> if the intrinsic was emitted, <see langword="false"/> otherwise. </returns>
    public static bool TryEmitIntrinsic(MethodSymbol method, LIRFunction target)
    {
        foreach (var emitter in Emitters)
        {
            IntrinsicEmitterAttribute? emitterAttribute = emitter.GetType().GetCustomAttribute<IntrinsicEmitterAttribute>();

            if (emitterAttribute?.FullyQualifiedName != method.FullyQualifiedName)
                continue;

            target.StartBody(); // In the future, intrinsics will emit into functions marked as 'extern'
            emitter.EmitInto(target);

            return true;
        }

        return false;
    }
}
