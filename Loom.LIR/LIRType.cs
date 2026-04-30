
namespace Loom.LIR;

public abstract record LIRType
{
    public static readonly LIRIntType Int32 = new(32);
    public static readonly LIRVoidType Void = new();
}

public record LIRIntType(int Bits) : LIRType;

public record LIRVoidType() : LIRType;

public record LIRFunctionType(LIRType ReturnType, List<LIRType> ParameterTypes) : LIRType;