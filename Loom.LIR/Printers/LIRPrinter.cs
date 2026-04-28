
using Loom.LIR.Builders;
using System.Text;

namespace Loom.LIR.Printers;

public sealed class LIRPrinter
{
    public string Print(LIRCompilationUnit unit)
    {
        var sb = new StringBuilder();

        foreach (var function in unit.Functions)
        {
            sb.AppendLine(Print(function));
            sb.AppendLine();
        }

        return sb.ToString();
    }

    private string Print(LIRFunction function)
    {
        var sb = new StringBuilder();

        sb.AppendLine($"define {function.Name} {{");

        foreach (var inst in function.Instructions)
        {
            sb.Append("  ");
            sb.AppendLine(Print(inst));
        }

        sb.AppendLine("}");
        return sb.ToString();
    }

    public string Print(LIRInstruction inst)
    {
        var operands = string.Join(", ", inst.Operands.Select(PrintValue));

        var result = inst.Result is not null
            ? $"{PrintValue(inst.Result)} = "
            : "";

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
