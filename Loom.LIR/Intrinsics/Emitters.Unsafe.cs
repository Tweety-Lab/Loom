using Loom.LIR.Objects;

namespace Loom.LIR.Intrinsics;

[IntrinsicEmitter("Standard::Memory::Unsafe::AddressOf")]
internal sealed class AddressOfEmitter : IIntrinsicEmitter
{
    /// <inheritdoc />
    public void EmitInto(LIRFunction function)
    {
        LIRValue value = function.ParameterValues[0];

        LIRTempValue storage = function.LIRGenerator!.EmitAlloca(value.Type);
        function.LIRGenerator.EmitStore(value, storage);
        function.LIRGenerator.EmitReturn(function.LIRGenerator.EmitPtrToInt(storage));
    }
}


[IntrinsicEmitter("Standard::Memory::Unsafe::Read")]
internal sealed class ReadEmitter : IIntrinsicEmitter
{
    /// <inheritdoc />
    public void EmitInto(LIRFunction function)
    {
        LIRValue address = function.ParameterValues[0];

        LIRTempValue pointer = function.LIRGenerator!.EmitIntToPtr(address, new LIRPointerType(function.Type.ReturnType));
        function.LIRGenerator.EmitReturn(function.LIRGenerator.EmitLoad(pointer));
    }
}