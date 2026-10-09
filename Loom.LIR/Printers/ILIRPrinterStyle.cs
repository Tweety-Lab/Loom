using Loom.LIR.Metadata;
using Loom.LIR.Objects;

namespace Loom.LIR.Printers;

/// <summary>
/// Base interface for LIR printer styles.
/// </summary>
public interface ILIRPrinterStyle
{
    string PrintMetaType(MetadataType type);
    string PrintMeta(LIRMetadataValue meta);

    string PrintType(LIRType type);
    string PrintValue(LIRValue value);
    string PrintInstruction(LIRInstruction inst);
    string PrintFunctionHeader(LIRFunction function);
    string PrintFunctionFooter();

    string PrintField(LIRField field);
    string PrintTypeDeclarationHeader(LIRTypeDeclaration structObj);
    string PrintTypeDeclarationFooter();
}
