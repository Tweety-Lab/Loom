using Loom.LIR.Metadata;
using Loom.LIR.Objects;

namespace Loom.LIR.Intrinsics;

[IntrinsicEmitter("Standard::Reflection::Assembly::get.Current")]
internal sealed class CurrentAssemblyEmitter : IIntrinsicEmitter
{
    /// <inheritdoc />
    public void EmitInto(LIRFunction function, LIRCompilationUnit unit)
    {
        var generator = function.LIRGenerator!;
        var assembly = unit.GetTypeDeclaration("Standard::Reflection::Assembly")!;
        var nameMetadata = unit.Metadata[MetadataType.Assembly].Single(value => value.Name == ".assembly.name");
        var characters = (LIRConstantArrayValue)nameMetadata.Initializer;

        var constructor = unit.GetFunction("Standard::Reflection::Assembly::.ctor.Standard::Memory::Slice<char>")!;
        var sliceType = constructor.Type.Parameters[1].Type;
        var sliceLayout = unit.GetTypeDeclaration("Standard::Memory::Slice")!;

        var slice = generator.EmitAlloca(sliceType);
        generator.EmitStore(generator.EmitPtrToInt(nameMetadata), generator.EmitGetField(slice, sliceLayout.Fields.Single(field => field.Name == "pointer")));
        generator.EmitStore(new LIRConstantIntValue(characters.Elements.Count), generator.EmitGetField(slice, sliceLayout.Fields.Single(field => field.Name == "Length.field")));

        var instance = generator.EmitAlloca(assembly.Type);
        generator.EmitCallInstanced(constructor, instance, generator.EmitLoad(slice));
        generator.EmitReturn(generator.EmitLoad(instance));
    }
}