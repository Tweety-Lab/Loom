using Loom.Analyzer;
using Loom.Analyzer.Symbols;
using Loom.LIR.Objects;
using Loom.Parser.AST;
using Loom.Parser.AST.Rules.Default;

namespace Loom.LIR.Generation;

internal partial class ExpressionGenerator
{
    /// <summary> Emits the initialization of <paramref name="address"/> from <paramref name="initializer"/>, defaulting the storage when there is no initializer. </summary>
    public void Initialize(LIRValue address, ExpressionNode? initializer, TypeSymbol type)
    {
        if (type is ArrayTypeSymbol array)
        {
            if (initializer is ArrayLiteralNode literal)
                EmitArrayLiteralInto(address, array, literal.Elements);
            else if (initializer is not DefaultExpressionNode && initializer != null && context.AnalysisContext.ExpressionTypes.TryGetValue(initializer, out var initializerType) && initializerType is ArrayTypeSymbol)
                Generator.EmitStore(EmitValue(initializer), address);
            else
                EmitDefaultArray(address, array);

            return;
        }

        Generator.EmitStore(initializer != null ? EmitValue(initializer, type) : EmitDefault(type), address);
    }

    /// <summary> Emits the default value of <paramref name="type"/>. </summary>
    public LIRValue EmitDefault(TypeSymbol type)
    {
        if (substitution != null)
            type = substitution.Resolve(type);

        if (type is ArrayTypeSymbol)
            throw new Exception("An array has no single default value; each of its elements is defaulted individually through EmitDefaultArray.");

        // An unsubstituted type parameter has no concrete default yet; its instantiation supplies one
        if (type is TypeParameterSymbol)
            return new LIRDefaultValue(ASTGenerator.ConvertType(type));

        if (!type.IsValueType)
            return new LIRNullValue(new LIRPointerType(ASTGenerator.ConvertType(type, substitution)));

        return type.KnownType switch
        {
            TypeSymbol.DefaultType.Char => new LIRConstantCharValue(0),
            TypeSymbol.DefaultType.I32 => new LIRConstantIntValue(0),
            TypeSymbol.DefaultType.Bool => new LIRConstantBoolValue(false),
            TypeSymbol.DefaultType.IPtr => new LIRConstantIntValue(0),
            _ => new LIRDefaultValue(ASTGenerator.ConvertType(type, substitution))
        };
    }

    /// <summary> Allocates a new instance and calls its constructor. </summary>
    /// <param name="node"> The instance creation to emit. </param>
    /// <param name="destination"> The address to construct into, or null to allocate a fresh instance. </param>
    public LIRValue EmitInstanceCreation(InstanceCreationExpresssionNode node, LIRValue? destination = null)
    {
        var symbol = context.AnalysisContext.ExpressionTypes[node] ?? throw new Exception("Could not resolve instance creation type.");
        symbol = substitution?.Resolve(symbol) ?? symbol;

        LIRPointerType instanceType = new(ASTGenerator.ConvertType(symbol, substitution));

        // A value type is constructed in place; a reference type is allocated on the heap so that it stays valid after
        var instance = destination ?? (symbol.IsValueType ? Generator.EmitAlloca(instanceType.PointeeType) : methodGenerator.AllocateInstance(function, instanceType));

        var constructor = context.AnalysisContext.GetSymbol(node).As<MethodSymbol>() ?? symbol.Members.OfType<MethodSymbol>().FirstOrDefault(m => m.Kind == MethodSymbol.MethodKind.Constructor);

        var target = constructor == null ? GetDefaultConstructorTarget(symbol) : GetCallTarget(constructor);
        var args = constructor == null ? node.Arguments.Select(EmitValue).ToArray() : EmitArguments(node.Arguments, constructor);

        Generator.EmitCallInstanced(target, instance, args);

        return instance;
    }

    private LIRValue GetDefaultConstructorTarget(TypeSymbol type)
    {
        if (type is not ConstructedTypeSymbol)
            return unit.GetFunction($"{type.FullyQualifiedName}::.ctor") ?? throw new Exception($"Could not find constructor for {type.FullyQualifiedName}.");

        var signature = new LIRFunctionType(LIRType.Void, [new LIRParameter("self", new LIRPointerType(ASTGenerator.ConvertType(type)))], []);

        return new LIRDefaultConstructorReference(type, signature);
    }

    /// <summary> Emits the declared initializer of every field of the type containing <paramref name="contextNode"/> into the instance being constructed. </summary>
    /// <remarks> Fields declared without an initializer are defaulted. </remarks>
    public void EmitFieldInitializers(ASTNode contextNode)
    {
        TypeSymbol instanceType = ResolveOwnerType(contextNode);

        var self = EmitSelfParameter();

        foreach (var field in instanceType.Members.OfType<FieldSymbol>())
        {
            if (field.Type == null)
                continue;

            var address = EmitFieldAddress(self, instanceType, field);
            var initializer = (field.DeclaringNode as FieldDeclarationNode)?.Variable.Initializer;

            Initialize(address, initializer, field.Type);
        }

        // A property initializes its hidden backing field through its initializer
        foreach (var property in instanceType.Members.OfType<PropertySymbol>())
        {
            if (property.IsStatic || property.BackingField?.Type == null)
                continue;

            var address = EmitFieldAddress(self, instanceType, property.BackingField);
            var initializer = (property.DeclaringNode as PropertyDeclarationNode)?.Initializer;

            Initialize(address, initializer, property.BackingField.Type);
        }
    }

    /// <summary> Emits a store of the default value of <paramref name="type"/> into every element of the array at <paramref name="arrayAddress"/> from <paramref name="from"/> onwards. </summary>
    public void EmitDefaultArray(LIRValue arrayAddress, ArrayTypeSymbol type, int from = 0)
    {
        for (int index = from; index < type.Size; index++)
        {
            var elementAddress = Generator.EmitGetElement(arrayAddress, new LIRConstantIntValue(index));

            if (type.ElementType is ArrayTypeSymbol nested)
                EmitDefaultArray(elementAddress, nested);
            else
                Generator.EmitStore(EmitDefault(type.ElementType), elementAddress);
        }
    }

    private LIRValue EmitArrayLiteralInto(LIRValue arrayAddress, ArrayTypeSymbol type, IReadOnlyList<ExpressionNode> elements)
    {
        for (int index = 0; index < Math.Min(elements.Count, type.Size); index++)
            Generator.EmitStore(EmitValue(elements[index], type.ElementType), Generator.EmitGetElement(arrayAddress, new LIRConstantIntValue(index)));

        EmitDefaultArray(arrayAddress, type, elements.Count);

        return arrayAddress;
    }
}
