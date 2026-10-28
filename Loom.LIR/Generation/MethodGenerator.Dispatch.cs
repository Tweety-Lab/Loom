using Loom.Analyzer.Symbols;
using Loom.LIR.Objects;

namespace Loom.LIR.Generation;

internal partial class MethodGenerator
{
    private LIRVirtualMethodReference GetInterfaceMethodReference(MethodSymbol method, TypeSymbol contract)
    {
        if (method.IsGeneric)
            throw new NotSupportedException("Generic interface methods require specialized dispatch slots.");

        var slots = InterfaceDispatchLayout.Methods(contract).ToArray();
        var slot = Array.FindIndex(slots, candidate => LinkageNameResolver.LinkageName(candidate) == LinkageNameResolver.LinkageName(method));
        if (slot < 0)
            throw new InvalidOperationException($"Interface '{contract.FullyQualifiedName}' has no dispatch slot for '{method.FullyQualifiedName}'.");

        var targetSignature = ASTGenerator.BuildFunctionType(method, new LIRPointerType(LIRType.Void), typeParameters: []);
        return new LIRVirtualMethodReference(slot, targetSignature);
    }
}
