using Loom.Analyzer;
using Loom.Analyzer.Symbols;
using Loom.Common;
using Loom.LIR.Objects;
using Loom.Parser.AST;
using Loom.Parser.AST.Rules.Default;

namespace Loom.LIR.Generation;

internal partial class ExpressionGenerator
{
    /// <summary> Emits the storage of a new array literal, or initializes <paramref name="destination"/> when one is given. </summary>
    /// <param name="node"> The array literal to emit. </param>
    /// <param name="destination"> The storage to initialize, or null to allocate a new array. </param>
    public LIRValue EmitArrayLiteral(ArrayLiteralNode node, LIRValue? destination = null)
    {
        if (context.AnalysisContext.ExpressionTypes.TryGetValue(node, out var resolved) && resolved is ArrayTypeSymbol type)
            return destination ?? EmitArrayLiteralInto(Generator.EmitAlloca(ASTGenerator.ConvertType(type, substitution)), type, node.Elements);

        throw new Exception($"Could not resolve the type of an array literal of {node.Elements.Count} element(s).");
    }

    /// <summary> Emits a string literal as a new slice whose pointer points at the literal's constant storage and whose length is its number of characters. </summary>
    public LIRValue EmitStringLiteral(StringLiteralNode node)
    {
        if (context.AnalysisContext.ExpressionTypes.TryGetValue(node, out var sliceType) && sliceType.IsValueType)
        {
            var stringAddress = EmitStringConstant(node.Value);

            // A slice literal is built through the generic definition, whose declaration exists before monomorphization
            TypeSymbol layout = sliceType is ConstructedTypeSymbol instantiated ? instantiated.OriginalDefinition : sliceType;

            var declarationType = new LIRTypeDeclarationType(layout.FullyQualifiedName, layout.IsValueType);
            var declaration = unit.TypeDeclarations.FirstOrDefault(t => t.Type == declarationType) ?? throw new Exception($"Could not find type: {layout.FullyQualifiedName}");

            var sliceAddress = Generator.EmitAlloca(ASTGenerator.ConvertType(sliceType, substitution));
            Generator.EmitStore(stringAddress, Generator.EmitGetField(sliceAddress, declaration.Fields.First(f => f.Name == "pointer")));
            Generator.EmitStore(new LIRConstantIntValue(node.Value.Length), Generator.EmitGetField(sliceAddress, declaration.Fields.First(f => f.Name == "Length.field")));

            return sliceAddress;
        }

        throw new Exception($"Could not resolve the type of a string literal of {node.Value.Length} character(s).");
    }

    // Emits the constant metadata value holding the characters of a string literal, reusing an identical one when the unit already declares it
    private LIRMetadataValue EmitStringConstant(int[] characters)
    {
        var charType = (TypeSymbol)context.AnalysisContext.Binders.First().Value.Lookup("char")!.First();

        var elements = new List<LIRValue>(characters.Select(character => (LIRValue)new LIRConstantCharValue(character)));

        // An empty literal still holds one element so that its address is valid to index
        if (elements.Count == 0)
            elements.Add(new LIRConstantCharValue(0));

        var initializer = new LIRConstantArrayValue(ASTGenerator.ConvertType(charType, substitution), elements);

        LIRMetadataValue? stringMetadata = unit.Metadata[Metadata.MetadataType.Strings].FirstOrDefault(g => g.IsConstant && g.Initializer.Equals(initializer));
        if (stringMetadata == null)
        {
            stringMetadata = LIRMetadataValue.Define($".str.{unit.Metadata[Metadata.MetadataType.Strings].Count}", initializer.Type, initializer);
            unit.Metadata[Metadata.MetadataType.Strings].Add(stringMetadata);
        }

        return stringMetadata;
    }
}
