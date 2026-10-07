
namespace Loom.LIR.Intrinsics;

[AttributeUsage(AttributeTargets.Class)]
internal class IntrinsicEmitterAttribute : Attribute
{
    /// <summary> The fully qualified name of the method this emitter operates on. </summary>
    public string FullyQualifiedName { get; }

    /// <summary> Initializes a new instance of the <see cref="IntrinsicEmitterAttribute"/> class. </summary>
    public IntrinsicEmitterAttribute(string fullyQualifiedName) => FullyQualifiedName = fullyQualifiedName;
}
