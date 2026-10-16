using Loom.Analyzer.Symbols;

namespace Loom.LIR.Objects;

/// <summary> A method instance used as a call operand before its concrete body has been materialized. </summary>
internal sealed class LIRMethodReference(string name, MethodSymbol method, LIRFunctionType type) : LIRFunctionReference(name, type)
{
    /// <summary> The substituted method signature whose body is requested. </summary>
    public MethodSymbol Method { get; } = method;
}
