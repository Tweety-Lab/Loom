
using Loom.LIR.Builders;
using System.Text;

namespace Loom.LIR.Printers;

public class LIRPrinter
{
    /// <summary> The currently used printer style. </summary>
    public ILIRPrinterStyle Style { get; }

    /// <summary> Initializes a new instance of the <see cref="LIRPrinter"/> class. </summary>
    public LIRPrinter(ILIRPrinterStyle style) => Style = style;

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

        sb.AppendLine(Style.PrintFunctionHeader(function));

        foreach (var inst in function.Instructions)
        {
            sb.AppendLine("  " + Style.PrintInstruction(inst));
        }

        sb.AppendLine(Style.PrintFunctionFooter());

        return sb.ToString();
    }
}
