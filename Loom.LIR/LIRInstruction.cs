using Loom.Analyzer.Symbols;
using Loom.LIR.OpCodes;
using Loom.Parser.AST.Rules.Default;

namespace Loom.LIR;

public record LIRInstruction(LIROpCode OpCode, List<LIRValue> Operands)
{
    /// <summary> The result, or null if this instruction produces no value. </summary>
    public LIRValue? Result { get; init; }

    /// <summary> The call expression this instruction was emitted for, or null when it does not originate from a call. </summary>
    public CallExpressionNode? Origin { get; init; }

    /// <summary> The concrete type arguments a generic call binds its template to, or null when the call is concrete. </summary>
    public IReadOnlyList<TypeSymbol>? TypeArguments { get; init; }
}
