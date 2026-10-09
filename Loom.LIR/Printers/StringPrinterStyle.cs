using Loom.LIR.Metadata;
using Loom.LIR.Objects;
using Loom.LIR.OpCodes;
using Loom.Parser.Literals;

namespace Loom.LIR.Printers;

/// <summary>
/// The default string printer for Loom Intermediate Representation.
/// </summary>
internal class StringPrinterStyle : ILIRPrinterStyle
{
    /// <inheritdoc/>
    public string PrintMetaType(MetadataType type) => $"META {type.ToString()}:";

    /// <inheritdoc/>
    public string PrintMeta(LIRMetadataValue meta) => $"{(meta.IsConstant ? "constant" : "meta")} {PrintType(meta.ValueType)} @{meta.Name} = {PrintValue(meta.Initializer)}";

    /// <inheritdoc/>
    public string PrintFunctionFooter() => "}";

    /// <inheritdoc/>
    public string PrintTypeDeclarationHeader(LIRTypeDeclaration s) => s.Type.IsValueType ? $"struct {s.Name}" : $"class {s.Name}";

    /// <inheritdoc/>
    public string PrintTypeDeclarationFooter() => "}";

    /// <inheritdoc/>
    public string PrintField(LIRField field) => $"field {PrintType(field.Type)} {field.Name}";

    /// <inheritdoc/>
    public string PrintFunctionHeader(LIRFunction f)
    {
        var sig = f.Type;
        var typeParameters = string.Join(", ", sig.TypeParameters.Select(tp => tp.Name));
        var typeParameterSuffix = typeParameters.Length > 0 ? $"<{typeParameters}>" : "";
        var parameters = string.Join(", ", sig.Parameters.Select(p => $"{PrintType(p.Type)} %{p.Name}"));
        var keyword = f.IsDeclaration ? "declare" : "define";

        return $"{keyword} {f.Name}{typeParameterSuffix}({parameters}) -> {PrintType(sig.ReturnType)}";
    }

    /// <inheritdoc/>
    public string PrintInstruction(LIRInstruction inst)
    {
        var result = inst.Result is not null ? $"{PrintValue(inst.Result)} = " : "";

        switch (inst.OpCode)
        {
            case var op when op == LIROpCode.Alloca:
                {
                    var ptrType = (LIRPointerType)inst.Result!.Type;
                    return $"{result}alloca {PrintType(ptrType.PointeeType)}";
                }

            case var op when op == LIROpCode.Store:
                {
                    var value = inst.Operands[0];
                    var ptr = inst.Operands[1];
                    var ptrType = (LIRPointerType)ptr.Type;

                    return $"store {PrintType(ptrType.PointeeType)} {PrintValue(value)}, {PrintValue(ptr)}";
                }

            case var op when op == LIROpCode.Load:
                {
                    var ptr = inst.Operands[0];
                    var ptrType = (LIRPointerType)ptr.Type;

                    return $"{result}load {PrintType(ptrType.PointeeType)}, {PrintValue(ptr)}";
                }

            case var op when op == LIROpCode.GetField:
                {
                    var ptrType = (LIRPointerType)inst.Result!.Type;
                    return $"{result}getfield {PrintType(ptrType.PointeeType)} {PrintValue(inst.Operands[0])}, {PrintValue(inst.Operands[1])}";
                }

            case var op when op == LIROpCode.GetElement:
                {
                    var ptrType = (LIRPointerType)inst.Result!.Type;
                    return $"{result}getelement {PrintType(ptrType.PointeeType)} {PrintValue(inst.Operands[0])}, {PrintValue(inst.Operands[1])}";
                }

            default:
                {
                    var type = inst.Result is not null ? $"{PrintType(inst.Result.Type)} " : "";

                    var operands = string.Join(", ", inst.Operands.Select(PrintValue));
                    return $"{result}{inst.OpCode.Name} {type}{operands}".TrimEnd();
                }
        }
    }

    /// <inheritdoc/>
    public string PrintType(LIRType type) => type switch
    {
        LIRCharType => "char",
        LIRIntPtrType => "iptr",
        LIRIntType i => $"i{i.Bits}",
        LIRVoidType => "void",
        LIRBoolType => "bool",
        LIRPointerType p => $"{PrintType(p.PointeeType)}*",
        LIRArrayType a => $"[{a.Size} x {PrintType(a.ElementType)}]",
        LIRTypeDeclarationType s => s.Name,
        LIRTypeParameter p => p.Name,
        _ => type.ToString()
    };

    /// <inheritdoc/>
    public string PrintValue(LIRValue value)
    {
        return value switch
        {
            LIRConstantCharValue c => CharacterLiteralParser.Format(c.Value),
            LIRConstantArrayValue a => $"[{string.Join(", ", a.Elements.Select(PrintValue))}]",
            LIRConstantIntValue c => $"{c.Value}",
            LIRConstantBoolValue c => c.Value ? "true" : "false",
            LIRNullValue => "null",
            LIRDefaultValue d => $"default({PrintType(d.Type)})",
            LIRTempValue t => $"%{t.ID}",
            LIRFunction f => $"@{f.Name}",
            LIRMetadataValue g => $"@{g.Name}",
            LIRField field => field.Name,
            LIRBlockValue b => $"%{b.Block.Name}",
            LIRTypeValue p => PrintType(p.Type),
            null => "%null",
            _ => $"%unknown:{value.Type}"
        };
    }
}
