
using Loom.Analyzer.Symbols;
using Loom.LIR.Builders;
using Loom.LIR.Objects;

namespace Loom.LIR;

/// <summary>
/// Context that holds semantic mappings for Loom Intermediate Representation (LIR).
/// </summary>
public class LIRSemanticContext
{
    public Dictionary<MethodDefinitionSymbol, FunctionBuilder> Functions { get; } = new();
    public Dictionary<Symbol, LIRValue> LocalVariables { get; } = new();
}
