
namespace Loom.LIR;

public abstract record LIRType
{
    public static readonly LIRIntType Int32 = new(32);
    public static readonly LIRIntType Int64 = new(64);
    public static readonly LIRIntPtrType IntPtr = new();
    public static readonly LIRVoidType Void = new();
    public static readonly LIRBoolType Boolean = new();
    public static readonly LIRCharType Char = new();
}

public record LIRIntType(int Bits) : LIRType;
public record LIRIntPtrType() : LIRType;

public record LIRVoidType() : LIRType;
public record LIRBoolType() : LIRType;

public record LIRCharType() : LIRType;

public record LIRPointerType(LIRType PointeeType) : LIRType;
public record LIRArrayType(LIRType ElementType, int Size) : LIRType;
public record LIRTypeDeclarationType(string Name, bool IsValueType) : LIRType;

public record LIRParameter(string Name, LIRType Type);
public record LIRTypeParameter(string Name) : LIRType;
public record LIRFunctionType(LIRType ReturnType, LIRParameter[] Parameters, LIRTypeParameter[] TypeParameters) : LIRType;
