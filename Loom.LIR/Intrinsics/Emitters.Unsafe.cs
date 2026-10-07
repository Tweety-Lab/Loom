using Loom.LIR.Objects;

namespace Loom.LIR.Intrinsics;

[IntrinsicEmitter("Standard::Memory::Unsafe::AddressOf")]
internal sealed class AddressOfEmitter : IIntrinsicEmitter
{
    /// <inheritdoc />
    public void EmitInto(LIRFunction function)
    {
        if (function.Type.Parameters.Length != 1)
            throw new InvalidOperationException($"{function.Name} expects exactly one parameter.");

        LIRValue value = function.ParameterValues[0];

        LIRTempValue storage = function.LIRGenerator!.EmitAlloca(value.Type);
        function.LIRGenerator.EmitStore(value, storage);
        function.LIRGenerator.EmitReturn(function.LIRGenerator.EmitPtrToInt(storage));
    }
}