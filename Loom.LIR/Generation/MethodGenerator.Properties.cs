using Loom.Analyzer.Symbols;
using Loom.LIR.Objects;

namespace Loom.LIR.Generation;

internal partial class MethodGenerator
{
    /// <summary> Declares the backing field and the accessors of the property <paramref name="symbol"/>. </summary>
    public void GeneratePropertyDeclaration(PropertySymbol symbol, LIRTypeDeclaration owningType, IReadOnlyList<TypeParameterSymbol>? typeParameters = null, TypeSubstitution? substitution = null)
    {
        if (symbol.BackingField?.Type != null)
            owningType.DeclareField(symbol.BackingField.Name, ASTGenerator.ConvertStorageType(symbol.BackingField.Type, substitution));

        DeclareAccessor(symbol.Getter, owningType, typeParameters, substitution);
        DeclareAccessor(symbol.Setter, owningType, typeParameters, substitution);
    }

    /// <summary> Emits the auto-implemented bodies of the accessors of the property <paramref name="symbol"/>. </summary>
    public void GeneratePropertyBody(PropertySymbol symbol, LIRTypeDeclaration owningType, TypeSubstitution? substitution = null, TypeSymbol? ownerType = null)
    {
        GenerateAccessorBody(symbol, symbol.Getter, owningType, false);
        GenerateAccessorBody(symbol, symbol.Setter, owningType, true);
    }

    // TODO: Emit the declared accessor body once it is supported instead of the auto-implemented field access
    private void GenerateAccessorBody(PropertySymbol property, MethodSymbol? accessor, LIRTypeDeclaration owningType, bool isSetter)
    {
        if (accessor == null || accessor.IsStatic || property.BackingField?.Type == null)
            return;

        LIRFunction function = GetFunction(LinkageNameResolver.LinkageName(accessor), owningType);
        LIRGenerator generator = function.LIRGenerator!;

        LIRField field = owningType.Fields.FirstOrDefault(f => f.Name == property.BackingField.Name) ?? throw new Exception($"Could not find field: {property.BackingField.Name}");

        var address = generator.EmitGetField(function.ParameterValues[0], field);

        if (isSetter)
            generator.EmitStore(function.ParameterValues[1], address);
        else
            generator.EmitReturn(generator.EmitLoad(address));

        EmitImplicitReturn(function);
    }

    private void DeclareAccessor(MethodSymbol? accessor, LIRTypeDeclaration owningType, IReadOnlyList<TypeParameterSymbol>? typeParameters, TypeSubstitution? substitution)
    {
        if (accessor == null)
            return;

        LIRType? selfType = accessor.IsStatic ? null : new LIRPointerType(owningType.Type);
        owningType.DefineMethod(LinkageNameResolver.LinkageName(accessor), ASTGenerator.BuildFunctionType(accessor, selfType, substitution, typeParameters));
    }
}
