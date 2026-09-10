using Loom.LIR.Objects;
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

        sb.Append(PrintMeta(unit));
        sb.AppendLine();

        foreach (var structType in unit.Structs)
        {
            sb.AppendLine(Print(structType));
            sb.AppendLine();
        }

        foreach (var function in unit.Functions)
        {
            sb.AppendLine(Print(function));
            sb.AppendLine();
        }

        return sb.ToString();
    }

    private string Print(LIRStructType structType) => $"struct {Style.PrintType(structType)};";

    private string Print(LIRFunction function)
    {
        var sb = new StringBuilder();

        sb.Append(PrintMeta(function));

        sb.AppendLine(Style.PrintFunctionHeader(function));

        if (!function.IsDeclaration)
        {
            foreach (var block in function.Blocks)
                sb.AppendLine(PrintBlock(block));

            sb.AppendLine(Style.PrintFunctionFooter());
        }

        return sb.ToString();
    }


    private string PrintBlock(LIRBasicBlock block)
    {
        var sb = new StringBuilder();

        sb.AppendLine($"{block.Name}:");

        foreach (var inst in block.Instructions)
            sb.AppendLine("  " + Style.PrintInstruction(inst));

        return sb.ToString();
    }

    private string PrintMeta(ILIRObject obj)
    {
        var sb = new StringBuilder();

        foreach (var meta in obj.MetaData)
            sb.AppendLine(Style.PrintMeta(meta.Key, meta.Value));

        return sb.ToString();
    }
}
