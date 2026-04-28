
using System.Text;

namespace Loom.LIR.Printers;

public sealed class LIRPrinter
{
    public string Print(List<LIRInstruction> instructions)
    {
        var sb = new StringBuilder();

        foreach (var inst in instructions)
            sb.AppendLine(Print(inst));

        return sb.ToString();
    }

    public string Print(LIRInstruction inst)
    {
        var operands = string.Join(", ", inst.Operands.Select(PrintValue));

        var result = inst.Result is not null ? $"{PrintValue(inst.Result)} = " : "";

        return $"{result}{inst.OpCode.Name} {operands}".TrimEnd();
    }

    private static string PrintValue(LIRValue value)
    {
        ArgumentNullException.ThrowIfNull(value);

        return value switch
        {
            LIRConstantValue c => c.Value?.ToString() ?? "null",
            _ => value.ToString()
        };
    }
}
