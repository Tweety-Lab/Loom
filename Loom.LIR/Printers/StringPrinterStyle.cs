using Loom.LIR.Objects;
using Loom.LIR.OpCodes;

namespace Loom.LIR.Printers;

/// <summary>
/// The default string printer for Loom Intermediate Representation.
/// </summary>
internal class StringPrinterStyle : ILIRPrinterStyle
{
    /// <inheritdoc/>
    public string PrintFunctionFooter() => "}";

    /// <inheritdoc/>
    public string PrintDeclaredObjectHeader(LIRDeclaredObject s) => s.Type.IsValueType ? $"struct {s.Name}" : $"class {s.Name}";

    /// <inheritdoc/>
    public string PrintDeclaredObjectFooter() => "}";

    /// <inheritdoc/>
    public string PrintField(LIRField field) => $"field {PrintType(field.Type)} {field.Name}";

    /// <inheritdoc/>
    public string PrintMeta(string key, string value) => $"[{key}: {value}]";

    /// <inheritdoc/>
    public string PrintFunctionHeader(LIRFunction f)
    {
        var sig = f.Type;
        var parameters = string.Join(", ", sig.Parameters.Select(p => $"{PrintType(p.Type)} %{p.Name}"));
        var keyword = f.IsDeclaration ? "declare" : "define";
        return $"{keyword} {f.Name}({parameters}) -> {PrintType(sig.ReturnType)}";
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
        LIRIntType i => $"i{i.Bits}",
        LIRVoidType => "void",
        LIRBoolType => "bool",
        LIRPointerType p => $"{PrintType(p.PointeeType)}*",
        LIRDeclaredObjectType s => s.Name,
        _ => type.ToString()
    };

    /// <inheritdoc/>
    public string PrintValue(LIRValue value)
    {
        return value switch
        {
            LIRConstantCharValue c => $"'{c.Value}'",
            LIRConstantIntValue c => $"{c.Value}",
            LIRConstantBoolValue c => c.Value ? "true" : "false",
            LIRTempValue t => $"%{t.ID}",
            LIRFunction f => $"@{f.Name}",
            LIRField field => field.Name,
            LIRBlockValue b => $"%{b.Block.Name}",
            null => "%null",
            _ => $"%unknown:{value.Type}"
        };
    }
}
