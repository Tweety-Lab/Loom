using Loom.LIR.Objects;

namespace Loom.LIR.Printers;

/// <summary>
/// The default string printer for Loom Intermediate Representation.
/// </summary>
internal class StringPrinterStyle : ILIRPrinterStyle
{
    /// <inheritdoc/>
    public string PrintFunctionFooter() => "}";

    /// <inheritdoc/>
    public string PrintMeta(string key, string value) => $"[{key}: {value}]";

    /// <inheritdoc/>
    public string PrintFunctionHeader(LIRFunction f)
    {
        var sig = f.Type;
        var parameters = string.Join(", ", sig.Parameters.Select(p => $"{PrintType(p.Type)} %{p.Name}"));
        return $"define {f.Name}({parameters}) -> {PrintType(sig.ReturnType)} {{";
    }

    /// <inheritdoc/>
    public string PrintInstruction(LIRInstruction inst)
    {
        var operands = string.Join(", ", inst.Operands.Select(PrintValue));
        var result = inst.Result is not null ? $"{PrintValue(inst.Result)} = " : "";
        return $"{result}{inst.OpCode.Name} {operands}".TrimEnd();
    }

    /// <inheritdoc/>
    public string PrintType(LIRType type) => type switch
    {
        LIRIntType i => $"i{i.Bits}",
        LIRVoidType => "void",
        LIRBoolType => "bool",
        _ => type.ToString()
    };

    /// <inheritdoc/>
    public string PrintValue(LIRValue value) =>
        value switch
        {
            LIRConstantIntValue c => c.ToString() ?? "null",
            _ => value.ToString()
        };
}
