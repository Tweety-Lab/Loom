using Loom.Analyzer.Symbols;
using Loom.LIR.Intrinsics;
using Loom.LIR.Metadata;
using Loom.LIR.Objects;

namespace Loom.LIR.Generation;

internal partial class MethodGenerator
{
    /// <summary> Declares the backing field and the accessors of the property <paramref name="symbol"/>. </summary>
    public void GeneratePropertyDeclaration(PropertySymbol symbol, LIRTypeDeclaration owningType, IReadOnlyList<TypeParameterSymbol>? typeParameters = null, TypeSubstitution? substitution = null)
    {
        if (symbol.BackingField?.Type != null)
        {
            var storageType = ASTGenerator.ConvertStorageType(symbol.BackingField.Type, substitution);
            if (!symbol.IsStatic)
                owningType.DeclareField(symbol.BackingField.Name, storageType);
            else if (symbol.ContainingType?.ContainsTypeParameters != true)
                unit.Metadata[MetadataType.Static].Add(LIRMetadataValue.Define(
                    $"{owningType.Name}::{symbol.BackingField.Name}", storageType, new LIRDefaultValue(storageType), isConstant: false));
        }

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
        if (accessor == null || (accessor.IsStatic && property.ContainingType?.ContainsTypeParameters == true))
            return;

        LIRFunction function = GetFunction(LinkageNameResolver.LinkageName(accessor), owningType);
        if (IIntrinsicEmitter.TryEmitIntrinsic(accessor, function, unit))
            return;

        if (property.BackingField?.Type == null)
            return;

        LIRGenerator generator = function.LIRGenerator!;

        LIRValue address;
        if (accessor.IsStatic)
            address = unit.Metadata[MetadataType.Static].Single(value => value.Name == $"{owningType.Name}::{property.BackingField.Name}");
        else
        {
            LIRField field = owningType.Fields.FirstOrDefault(f => f.Name == property.BackingField.Name) ?? throw new Exception($"Could not find field: {property.BackingField.Name}");
            address = generator.EmitGetField(function.ParameterValues[0], field);
        }

        if (isSetter)
            generator.EmitStore(function.ParameterValues[accessor.IsStatic ? 0 : 1], address);
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
