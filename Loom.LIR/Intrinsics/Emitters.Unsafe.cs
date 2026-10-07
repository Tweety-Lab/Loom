using Loom.LIR.Objects;

namespace Loom.LIR.Intrinsics;

[IntrinsicEmitter("Standard::Memory::Unsafe::TestIntrinsic")]
internal sealed class TestIntrinsicEmitter : IIntrinsicEmitter
{
    /// <inheritdoc />
    public void EmitInto(LIRFunction function)
    {
        var sizeOf = function.LIRGenerator?.EmitSizeOf(LIRType.Int32);
        function.LIRGenerator?.EmitReturn(sizeOf);
    }
}