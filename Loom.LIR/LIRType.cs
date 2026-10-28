
namespace Loom.LIR;

public abstract record LIRType
{
    // Worth thinking about if we should just have LIRType.Byte = LIRIntType(8, false) instead of having every LIRType carry signage info

    /// <summary> Whether integer operations treat this type as signed. </summary>
    /// <remarks> <see langword="false"/> when non-integer type. </remarks>
    public virtual bool IsSigned => false;

    public static readonly LIRIntType Int32 = new(32);
    public static readonly LIRIntType Int64 = new(64);
    public static readonly LIRIntPtrType IntPtr = new();
    public static readonly LIRVoidType Void = new();
    public static readonly LIRBoolType Boolean = new();
    public static readonly LIRCharType Char = new();
    public static readonly LIRByteType Byte = new();
}

public record LIRIntType(int Bits, bool Signed = true) : LIRType
{
    /// <inheritdoc/>
    public override bool IsSigned => Signed;
}

public record LIRIntPtrType() : LIRType
{
    /// <inheritdoc/>
    public override bool IsSigned => true;
}

public record LIRVoidType() : LIRType;
public record LIRBoolType() : LIRType;

public record LIRCharType() : LIRType
{
    /// <inheritdoc/>
    public override bool IsSigned => true;
}

public record LIRByteType() : LIRType;

public record LIRPointerType(LIRType PointeeType) : LIRType;
public record LIRArrayType(LIRType ElementType, int Size) : LIRType;
public record LIRTypeDeclarationType(string Name, bool IsValueType) : LIRType;

public record LIRParameter(string Name, LIRType Type);
public record LIRTypeParameter(string Name) : LIRType;
public record LIRFunctionType(LIRType ReturnType, LIRParameter[] Parameters, LIRTypeParameter[] TypeParameters) : LIRType;
