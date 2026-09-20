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

        foreach (var declaredObj in unit.DeclaredTypes)
        {
            sb.AppendLine(Print(declaredObj));
            sb.AppendLine();
        }

        foreach (var function in unit.Functions)
        {
            sb.AppendLine(Print(function));
            sb.AppendLine();
        }

        return sb.ToString();
    }

    private string Print(LIRDeclaredType declaredObj)
    {
        var sb = new StringBuilder();

        sb.Append(PrintMeta(declaredObj));

        sb.AppendLine(Style.PrintDeclaredTypeHeader(declaredObj));

        foreach (var field  in declaredObj.Fields)
        {
            sb.AppendLine("  " + Style.PrintField(field).Replace("\n", "\n  "));
            sb.AppendLine();
        }

        foreach (var method in declaredObj.Methods)
        {
            sb.AppendLine("  " + Style.PrintFunctionHeader(method).Replace("\n", "\n  "));
            foreach (var block in method.Blocks)
                sb.AppendLine(PrintBlock(block, "    "));

            if (!method.IsDeclaration)
                sb.AppendLine("  " + Style.PrintFunctionFooter());
        }

        sb.AppendLine(Style.PrintDeclaredTypeFooter());
        return sb.ToString();
    }

    private string Print(LIRFunction function)
    {
        var sb = new StringBuilder();

        sb.Append(PrintMeta(function));

        sb.AppendLine(Style.PrintFunctionHeader(function));

        if (!function.IsDeclaration)
        {
            foreach (var block in function.Blocks)
                sb.AppendLine(PrintBlock(block, "  "));

            sb.AppendLine(Style.PrintFunctionFooter());
        }

        return sb.ToString();
    }


    private string PrintBlock(LIRBasicBlock block, string indent = "  ")
    {
        var sb = new StringBuilder();

        sb.AppendLine($"{indent}{block.Name}:");

        foreach (var inst in block.Instructions)
            sb.AppendLine(indent + "  " + Style.PrintInstruction(inst));

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
