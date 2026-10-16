using Loom.Analyzer;
using Loom.Analyzer.Symbols;
using Loom.LIR.Objects;
using Loom.Parser.AST.Rules.Default;

namespace Loom.LIR.Generation;

// TODO: Remove this
// Partial class is a sign this class is getting to big
internal partial class ExpressionGenerator
{
    /// <summary> Emits a value with its implicit conversion to the required semantic type. </summary>
    public LIRValue EmitValue(ExpressionNode node, TypeSymbol targetType)
    {
        targetType = substitution?.Resolve(targetType) ?? targetType;
        var sourceType = context.AnalysisContext.ExpressionTypes.TryGetValue(node, out var type) ? substitution?.Resolve(type) ?? type : targetType;
        var value = TypeConversions.IsArrayToVectorConversion(sourceType, targetType) ? Emit(node) : EmitValue(node);

        return EmitConversion(value, sourceType, targetType);
    }

    private LIRValue EmitConversion(LIRValue value, TypeSymbol source, TypeSymbol target) => (source, target) switch
    {
        (ArrayTypeSymbol array, ConstructedTypeSymbol vector) when TypeConversions.IsArrayToVectorConversion(array, vector) => EmitArrayToVector(value, array, vector),
        (ArrayTypeSymbol array, ArrayTypeSymbol converted) when !TypeConversions.IsIdentical(array, converted) => EmitArrayConversion(value, array, converted),
        _ => Generator.EmitConvert(value, ASTGenerator.ConvertStorageType(target))
    };

    private LIRValue EmitArrayStorage(LIRValue value, ArrayTypeSymbol type)
    {
        if (value.Type is LIRPointerType { PointeeType: LIRArrayType })
            return value;

        var storage = Generator.EmitAlloca(ASTGenerator.ConvertType(type));

        Generator.EmitStore(value, storage);

        return storage;
    }

    private LIRValue EmitArrayConversion(LIRValue value, ArrayTypeSymbol source, ArrayTypeSymbol target)
    {
        var array = EmitArrayStorage(value, source);
        var converted = Generator.EmitAlloca(ASTGenerator.ConvertType(target));

        for (int index = 0; index < source.Size; index++)
        {
            var position = new LIRConstantIntValue(index);
            var element = Generator.EmitLoad(Generator.EmitGetElement(array, position));

            Generator.EmitStore(EmitConversion(element, source.ElementType, target.ElementType), Generator.EmitGetElement(converted, position));
        }

        return Generator.EmitLoad(converted);
    }

    private LIRValue EmitArrayToVector(LIRValue value, ArrayTypeSymbol arrayType, ConstructedTypeSymbol vectorType)
    {
        var array = EmitArrayStorage(value, arrayType);
        var elementType = vectorType.TypeArguments[0];
        var constructor = vectorType.Members.OfType<MethodSymbol>().FirstOrDefault(method => method is { Kind: MethodSymbol.MethodKind.Constructor, IsStatic: false, Parameters.Count: 0 });
        var add = vectorType.Members.OfType<MethodSymbol>().FirstOrDefault(method => method is { Name: "Add", Kind: MethodSymbol.MethodKind.Method, IsStatic: false, Arity: 0, Parameters: [var parameter] } && parameter.Type is { } type && TypeConversions.IsIdentical(type, elementType))
            ?? throw new InvalidOperationException($"'{vectorType.FullyQualifiedName}' must declare Add({elementType.Name}) for array conversion.");
        var vector = methodGenerator.AllocateInstance(function, new LIRPointerType(ASTGenerator.ConvertType(vectorType)));

        Generator.EmitCallInstanced(constructor == null ? GetDefaultConstructorTarget(vectorType) : GetCallTarget(constructor), vector);

        var index = Generator.EmitAlloca(LIRType.Int32);
        var condition = Generator.CreateBlock("array.vector.condition");
        var body = Generator.CreateBlock("array.vector.body");
        var continuation = Generator.CreateBlock("array.vector.continue");

        Generator.EmitStore(new LIRConstantIntValue(0), index);
        Generator.EmitBr(condition);
        Generator.SwitchTo(condition);

        var current = Generator.EmitLoad(index);

        Generator.EmitCondBr(Generator.EmitCmpLt(current, new LIRConstantIntValue(arrayType.Size)), body, continuation);
        Generator.SwitchTo(body);

        var element = Generator.EmitLoad(Generator.EmitGetElement(array, current));

        Generator.EmitCallInstanced(GetCallTarget(add), vector, EmitConversion(element, arrayType.ElementType, elementType));
        Generator.EmitStore(Generator.EmitAdd(current, new LIRConstantIntValue(1)), index);
        Generator.EmitBr(condition);
        Generator.SwitchTo(continuation);

        return vector;
    }
}
