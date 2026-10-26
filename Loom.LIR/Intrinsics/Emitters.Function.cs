using Loom.LIR.Objects;

namespace Loom.LIR.Intrinsics;

[IntrinsicEmitter("Standard::Function::Invoke")]
internal sealed class FunctionInvokeEmitter : IIntrinsicEmitter
{
    /// <inheritdoc/>
    public void EmitInto(LIRFunction function, LIRCompilationUnit unit)
    {
        var declaration = unit.TypeDeclarations.Single(type => type.Name == "Standard::Function");
        var generator = function.LIRGenerator!;
        var self = function.ParameterValues[0];
        var address = generator.EmitLoad(generator.EmitGetField(self, declaration.Fields.Single(field => field.Name == "pointer")));
        var signature = new LIRFunctionType(LIRType.Void, [], []);
        var target = generator.EmitIntToPtr(address, new LIRPointerType(signature));

        generator.EmitCall(target);
        generator.EmitReturn();
    }
}