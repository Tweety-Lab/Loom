using Loom.Analyzer.Symbols;

namespace Loom.LIR.Objects;

/// <summary> An implicit constructor referenced through its constructed declaring type. </summary>
internal sealed class LIRDefaultConstructorReference(TypeSymbol declaringType, LIRFunctionType type) : LIRFunctionReference($"{declaringType.FullyQualifiedName}::.ctor", type)
{
    /// <summary> The type whose implicit constructor is requested. </summary>
    public TypeSymbol DeclaringType { get; } = declaringType;
}
