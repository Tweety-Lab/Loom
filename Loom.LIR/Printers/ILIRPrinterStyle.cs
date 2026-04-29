using Loom.LIR.Builders;
using Loom.LIR.Objects;

namespace Loom.LIR.Printers;

/// <summary>
/// Base interface for LIR printer styles.
/// </summary>
public interface ILIRPrinterStyle
{
    string PrintMeta(string key, string value);
    string PrintType(LIRType type);
    string PrintValue(LIRValue value);
    string PrintInstruction(LIRInstruction inst);
    string PrintFunctionHeader(LIRFunction function);
    string PrintFunctionFooter();
}
