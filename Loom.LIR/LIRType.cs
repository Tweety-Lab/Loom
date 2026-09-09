
namespace Loom.LIR;

public abstract record LIRType
{
    public static readonly LIRIntType Int32 = new(32);
    public static readonly LIRIntType IntPtr = new(System.IntPtr.Size * 8);
    public static readonly LIRVoidType Void = new();
    public static readonly LIRBoolType Boolean = new();
}

public record LIRIntType(int Bits) : LIRType;

public record LIRVoidType() : LIRType;
public record LIRBoolType() : LIRType;
public record LIRPointerType(LIRType PointeeType) : LIRType;

public record LIRParameter(string Name, LIRType Type);
public record LIRFunctionType(LIRType ReturnType, LIRParameter[] Parameters) : LIRType;