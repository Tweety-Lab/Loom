using Loom.Analyzer;
using Loom.Analyzer.Symbols;
using Loom.Common;
using Loom.LIR.Objects;
using Loom.Parser.AST;
using Loom.Parser.AST.Rules.Default;

namespace Loom.LIR.Generation;

/// <summary>
/// Handles conversion of Abstract Syntax Tree (AST) expressions into Loom Intermediate Representation (LIR).
/// </summary>
internal class ExpressionGenerator
{
    private CompilationContext context;
    private LIRCompilationUnit unit;
    private LIRFunction function;
    private Dictionary<string, LIRTempValue> locals;
    private MethodGenerator methodGenerator;
    private TypeSubstitution? substitution;
    private TypeSymbol? ownerType;

    private LIRGenerator Generator => function.LIRGenerator!;

    /// <summary> Initializes a new instance of the <see cref="ExpressionGenerator"/> class. </summary>
    public ExpressionGenerator(CompilationContext context, LIRCompilationUnit unit, LIRFunction function, Dictionary<string, LIRTempValue> locals, MethodGenerator methodGenerator, TypeSubstitution? substitution = null, TypeSymbol? ownerType = null)
    {
        this.context = context;
        this.unit = unit;
        this.function = function;
        this.locals = locals;
        this.methodGenerator = methodGenerator;
        this.substitution = substitution;
        this.ownerType = ownerType;
    }

    public LIRValue Emit(ExpressionNode node) => node switch
    {
        CharacterLiteralNode character => new LIRConstantCharValue(character.Value),
        NumberLiteralNode num => new LIRConstantIntValue(num.Value),
        IdentifierNameNode ident => ResolveProperty(ident) is { } identProperty ? EmitPropertyGet(identProperty, ident, EmitPropertySelf(identProperty, ident)) : EmitVariableValue(ident),
        BooleanLiteralNode boolean => new LIRConstantBoolValue(boolean.Value),
        DefaultExpressionNode @default => EmitDefault(context.AnalysisContext.ExpressionTypes[@default]),
        BinaryExpressionNode binary => EmitBinary(binary),
        CallExpressionNode call => EmitCall(call),
        ArrayLiteralNode literal => EmitArrayLiteral(literal),
        StringLiteralNode str => EmitStringLiteral(str),
        ArrayAccessExpressionNode array => EmitVariableValue(array),
        MemberAccessExpressionNode member => ResolveProperty(member) is { } memberProperty ? EmitPropertyGet(memberProperty, member, EmitPropertySelf(memberProperty, member)) : EmitVariableValue(member),
        InstanceCreationExpresssionNode creation => EmitInstanceCreation(creation),
        SizeOfExpressionNode sizeOf => EmitSizeOf(sizeOf),
        IncrementExpressionNode increment => EmitIncrement(increment),
        _ => throw new Exception($"Unhandled expression: {node.GetType().Name}")
    };

    /// <summary> Emits the address of an addressable expression (a local variable or a field). </summary>
    public LIRValue EmitAddress(ExpressionNode node)
    {
        if (ResolveProperty(node) is { } property)
            return EmitPropertyAddress(property, node);

        return node switch
        {
            IdentifierNameNode ident => EmitIdentifierAddress(ident),
            MemberAccessExpressionNode member => EmitMemberAddress(member),
            ArrayAccessExpressionNode array => EmitArrayElementAddress(array),
            ArrayLiteralNode literal => EmitArrayLiteral(literal),
            StringLiteralNode str => EmitStringLiteral(str),
            InstanceCreationExpresssionNode creation => Emit(creation),
            _ => throw new Exception($"Unhandled addressable expression: {node.GetType().Name}")
        };
    }

    /// <summary> Emits an assignment to a property through its set accessor, or its backing field when it has none. </summary>
    public void EmitAssignment(AssignmentStatementNode node)
    {
        if (ResolveProperty(node.Target) is { } property)
        {
            EmitPropertySet(property, node.Target, node.Value);
            return;
        }

        var address = EmitAddress(node.Target);

        // Assigning to a whole array defaults each of its elements rather than storing a single value
        if (context.AnalysisContext.ExpressionTypes.TryGetValue(node.Target, out var targetType) && targetType is ArrayTypeSymbol)
        {
            Initialize(address, node.Value, targetType);
            return;
        }

        Generator.EmitStore(EmitValue(node.Value), address);
    }

    /// <summary> Emits <paramref name="node"/> as a value. </summary>
    public LIRValue EmitValue(ExpressionNode node)
    {
        var value = Emit(node);
        return value.Type is LIRPointerType pointer && IsValueType(pointer.PointeeType) ? Generator.EmitLoad(value) : value;
    }

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

        Generator.EmitStore(initializer != null ? EmitValue(initializer) : EmitDefault(type), address);
    }

    public LIRValue EmitSizeOf(SizeOfExpressionNode node)
    {
        TypeSymbol? type = (TypeSymbol?)context.AnalysisContext.GetSymbol(node.Type).Symbol;

        if (type == null)
            throw new Exception($"Could not resolve size of type {node.Type}.");

        return Generator.EmitSizeOf(ASTGenerator.ConvertType(type, substitution));
    }

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
        var args = node.Arguments.Select(EmitValue).ToArray();

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

    // Emits a pointer to the storage of the local variable or field referenced by 'ident'
    private LIRValue EmitStorage(IdentifierNameNode ident)
    {
        if (locals.TryGetValue(ident.BaseName, out var address))
            return address;

        var symbol = context.AnalysisContext.GetSymbol(ident).Symbol;
        if (symbol is FieldSymbol fieldSymbol)
            return EmitBareFieldAddress(fieldSymbol, ident);

        throw new Exception($"Unhandled identifier: {ident.BaseName}");
    }

    // Emits a pointer to the variable referenced by 'ident', which is the pointer it holds for a reference type
    private LIRValue EmitIdentifierAddress(IdentifierNameNode ident)
    {
        if (IsParameter(ident))
        {
            if (IsValueType(ident))
                throw new Exception($"Cannot take the address of a value type parameter: {ident.BaseName}");

            return EmitParameterValue(ident.BaseName);
        }

        var storage = EmitStorage(ident);
        return IsValueType(ident) ? storage : Generator.EmitLoad(storage);
    }

    // Emits the value of the variable referenced by 'ident'
    private LIRValue EmitVariableValue(IdentifierNameNode ident) => IsParameter(ident) ? EmitParameterValue(ident.BaseName) : Generator.EmitLoad(EmitStorage(ident));

    // Emits the value of the variable-like 'node', which is stored in memory
    private LIRValue EmitVariableValue(ExpressionNode node)
    {
        var address = EmitAddress(node);
        return IsValueType(node) ? Generator.EmitLoad(address) : address;
    }

    private LIRValue EmitArrayLiteralInto(LIRValue arrayAddress, ArrayTypeSymbol type, IReadOnlyList<ExpressionNode> elements)
    {
        for (int index = 0; index < Math.Min(elements.Count, type.Size); index++)
            Generator.EmitStore(EmitValue(elements[index]), Generator.EmitGetElement(arrayAddress, new LIRConstantIntValue(index)));

        EmitDefaultArray(arrayAddress, type, elements.Count);

        return arrayAddress;
    }

    private bool IsParameter(IdentifierNameNode ident) => !locals.ContainsKey(ident.BaseName) && context.AnalysisContext.GetSymbol(ident).Symbol is ParameterSymbol;

    private bool IsValueType(ExpressionNode node)
    {
        if (!context.AnalysisContext.ExpressionTypes.TryGetValue(node, out var type))
            return true;

        if (substitution != null)
            type = substitution.Resolve(type);

        // An unsubstituted type parameter is stored like a value type; its instantiation decides the real storage
        return type is TypeParameterSymbol || type.IsValueType;
    }

    private static bool IsValueType(LIRType type) => type switch
    {
        LIRArrayType => true,
        LIRTypeParameter => true,
        LIRTypeDeclarationType declaration => declaration.IsValueType,
        _ => false
    };

    private static bool IsWide(LIRType type) => type is LIRIntPtrType or LIRIntType { Bits: > 32 };

    private LIRValue EmitParameterValue(string name)
    {
        var index = Array.FindIndex(function.Type.Parameters, p => p.Name == name);

        if (index < 0)
            throw new Exception($"Could not find parameter: {name}");

        return function.ParameterValues[index];
    }

    private LIRValue EmitMemberAddress(MemberAccessExpressionNode node)
    {
        var receiverType = context.AnalysisContext.ExpressionTypes.TryGetValue(node.Receiver, out var type) ? type : null;
        if (receiverType == null)
            throw new Exception($"Could not resolve receiver type: {node.Receiver}");

        var fieldSymbol = receiverType.Members.OfType<FieldSymbol>().FirstOrDefault(m => m.Name == node.Name.BaseName) ?? throw new Exception($"Could not resolve member field: {node.Name.BaseName}");

        return EmitFieldAddress(EmitAddress(node.Receiver), receiverType, fieldSymbol);
    }

    // Resolves the property a variable-like expression reads or writes, if it is one
    private PropertySymbol? ResolveProperty(ExpressionNode node) => node switch
    {
        IdentifierNameNode identifier when !locals.ContainsKey(identifier.BaseName) && !IsParameter(identifier) => context.AnalysisContext.GetSymbol(identifier).Symbol as PropertySymbol,
        MemberAccessExpressionNode member => ResolveMember(member) as PropertySymbol,
        _ => null
    };

    private Symbol? ResolveMember(MemberAccessExpressionNode node)
    {
        var receiverType = context.AnalysisContext.ExpressionTypes.TryGetValue(node.Receiver, out var type) ? type : context.AnalysisContext.GetSymbol(node.Receiver).Symbol as TypeSymbol;
        return receiverType?.Members.FirstOrDefault(m => m.Name == node.Name.BaseName);
    }

    // Resolves the instance a property access acts on, or null when the property is static
    private LIRValue? EmitPropertySelf(PropertySymbol property, ExpressionNode node) => property.IsStatic ? null : node switch
    {
        MemberAccessExpressionNode member => EmitAddress(member.Receiver),
        _ => EmitSelfParameter()
    };

    private LIRValue EmitPropertyGet(PropertySymbol property, ASTNode node, LIRValue? self)
    {
        MethodSymbol getter = property.Getter ?? throw new Exception($"Could not resolve the get accessor of property '{property.Name}'.");
        return EmitAccessorCall(getter, self);
    }

    private LIRValue EmitPropertyAddress(PropertySymbol property, ExpressionNode node)
    {
        var value = EmitPropertyGet(property, node, EmitPropertySelf(property, node));

        // A reference type is already addressed by the pointer it holds; a value type needs storage to be addressed
        if (value.Type is LIRPointerType)
            return value;

        var address = Generator.EmitAlloca(value.Type);
        Generator.EmitStore(value, address);
        return address;
    }

    private void EmitPropertySet(PropertySymbol property, ExpressionNode target, ExpressionNode value) => EmitPropertySet(property, target, EmitPropertySelf(property, target), EmitValue(value));

    private void EmitPropertySet(PropertySymbol property, ExpressionNode target, LIRValue? self, LIRValue stored)
    {
        if (property.Setter != null)
        {
            EmitAccessorCall(property.Setter, self, stored);
            return;
        }

        // A get-only property is assigned through its backing field on the instance it belongs to
        TypeSymbol owner = target is MemberAccessExpressionNode member && context.AnalysisContext.ExpressionTypes.TryGetValue(member.Receiver, out var receiverType) ? receiverType : ResolveOwnerType(target);

        Generator.EmitStore(stored, EmitFieldAddress(self!, owner, property.BackingField!));
    }

    private LIRValue EmitIncrement(IncrementExpressionNode node)
    {
        if (ResolveProperty(node.Target) is { } property)
        {
            var self = EmitPropertySelf(property, node.Target);
            var oldPropertyValue = EmitPropertyGet(property, node.Target, self);
            var newPropertyValue = ApplyDelta(oldPropertyValue, node.Operator);

            EmitPropertySet(property, node.Target, self, newPropertyValue);

            return node.IsPrefix ? newPropertyValue : oldPropertyValue;
        }

        var address = EmitAddress(node.Target);
        var oldValue = Generator.EmitLoad(address);
        var newValue = ApplyDelta(oldValue, node.Operator);

        Generator.EmitStore(newValue, address);

        return node.IsPrefix ? newValue : oldValue;
    }

    private LIRValue ApplyDelta(LIRValue value, Parser.Tokenizer.Token op)
    {
        var one = Generator.EmitConvert(new LIRConstantIntValue(1), value.Type);

        return op.Type == Parser.Tokenizer.Token.TokenType.PlusPlus
            ? Generator.EmitAdd(value, one)
            : Generator.EmitSub(value, one);
    }

    private LIRValue EmitAccessorCall(MethodSymbol accessor, LIRValue? self, params LIRValue[] arguments) => self == null ? Generator.EmitCall(GetCallTarget(accessor), arguments) : Generator.EmitCallInstanced(GetCallTarget(accessor), self, arguments);

    private LIRValue EmitArrayElementAddress(ArrayAccessExpressionNode node)
    {
        var arrayAddress = EmitAddress(node.Receiver);
        return Generator.EmitGetElement(arrayAddress, EmitValue(node.Index));
    }

    private LIRValue EmitBareFieldAddress(FieldSymbol symbol, ASTNode contextNode) => EmitFieldAddress(EmitSelfParameter(), ResolveOwnerType(contextNode), symbol);

    private TypeSymbol ResolveOwnerType(ASTNode contextNode)
    {
        if (ownerType != null)
            return ownerType;

        var typeNode = context.AnalysisContext.FirstAncestorOrSelf<ITypeDeclarationNode>(contextNode);

        if (typeNode == null)
            throw new Exception($"Could not resolve declaring type: {contextNode.GetType().Name}");

        if (context.AnalysisContext.GetSymbol((ASTNode)typeNode).Symbol is not TypeSymbol instanceType)
            throw new Exception($"Could not resolve declaring type: {contextNode.GetType().Name}");

        return instanceType;
    }

    private LIRValue EmitSelfParameter()
    {
        if (!function.Type.Parameters.Any(p => p.Name == "self"))
            throw new Exception($"Field access requires an instance method with a 'self' parameter ({function.Name}).");

        return EmitParameterValue("self");
    }

    private LIRValue EmitFieldAddress(LIRValue instance, TypeSymbol instanceTypeSymbol, FieldSymbol fieldSymbol)
    {
        instanceTypeSymbol = substitution?.Resolve(instanceTypeSymbol) ?? instanceTypeSymbol;
        fieldSymbol = instanceTypeSymbol.Members.OfType<FieldSymbol>().FirstOrDefault(field => field.Name == fieldSymbol.Name) ?? fieldSymbol;
        // An instantiated generic type shares the layout of its definition, whose declaration exists before monomorphization
        TypeSymbol layout = instanceTypeSymbol is ConstructedTypeSymbol instantiated ? instantiated.OriginalDefinition : instanceTypeSymbol;

        var declarationType = new LIRTypeDeclarationType(layout.FullyQualifiedName, layout.IsValueType);
        var declaration = unit.TypeDeclarations.FirstOrDefault(s => s.Type == declarationType) ?? throw new Exception($"Could not find type: {layout.FullyQualifiedName}");

        var field = declaration.Fields.FirstOrDefault(f => f.Name == fieldSymbol.Name) ?? throw new Exception($"Could not find field: {fieldSymbol.Name}");

        // The concrete declaration does not exist yet, so the substituted field is rebuilt from the definition's layout
        if (instanceTypeSymbol is ConstructedTypeSymbol)
            field = LIRField.Declare(field.Name, field.Index, ASTGenerator.ConvertType(fieldSymbol.Type!, substitution));

        return Generator.EmitGetField(instance, field);
    }

    private LIRValue EmitBinary(BinaryExpressionNode node)
    {
        var left = Emit(node.Left);
        var right = Emit(node.Right);

        if (left.Type != right.Type && LIRGenerator.IsIntegerType(left.Type) && LIRGenerator.IsIntegerType(right.Type))
        {
            LIRType common = IsWide(left.Type) || !IsWide(right.Type) ? left.Type : right.Type;

            left = Generator.EmitConvert(left, common);
            right = Generator.EmitConvert(right, common);
        }

        return node.Operator.Type switch
        {
            Parser.Tokenizer.Token.TokenType.Plus => Generator.EmitAdd(left, right),
            Parser.Tokenizer.Token.TokenType.Minus => Generator.EmitSub(left, right),
            Parser.Tokenizer.Token.TokenType.Star => Generator.EmitMul(left, right),
            Parser.Tokenizer.Token.TokenType.Slash => Generator.EmitDiv(left, right),
            Parser.Tokenizer.Token.TokenType.EqualEqual => Generator.EmitCmpEq(left, right),
            Parser.Tokenizer.Token.TokenType.NotEqual => Generator.EmitCmpNe(left, right),
            Parser.Tokenizer.Token.TokenType.Less => Generator.EmitCmpLt(left, right),
            Parser.Tokenizer.Token.TokenType.Greater => Generator.EmitCmpGt(left, right),
            Parser.Tokenizer.Token.TokenType.LessEqual => Generator.EmitCmpLe(left, right),
            Parser.Tokenizer.Token.TokenType.GreaterEqual => Generator.EmitCmpGe(left, right),
            _ => throw new Exception($"Unhandled operator: {node.Operator.Text}")
        };
    }

    private LIRValue EmitCall(CallExpressionNode node)
    {
        LIRValue? self = null;
        MethodSymbol methodSymbol = context.AnalysisContext.GetSymbol(node).As<MethodSymbol>() ?? throw new Exception($"Could not resolve call: {node.Callee}");

        if (node.Callee is MemberAccessExpressionNode member)
        {
            var receiverIsValue = context.AnalysisContext.ExpressionTypes.TryGetValue(member.Receiver, out var receiverType);
            if (!receiverIsValue)
                receiverType = context.AnalysisContext.GetSymbol(member.Receiver).Symbol as TypeSymbol;

            if (!receiverIsValue && !methodSymbol.IsStatic)
                throw new Exception($"Member '{member.Name.BaseName}' is not static and cannot be called on type '{receiverType?.Name}'.");

            if (receiverIsValue)
                self = EmitAddress(member.Receiver);
        }

        var target = GetCallTarget(methodSymbol);

        if (self == null && ((LIRFunctionType)target.Type).Parameters.FirstOrDefault()?.Name == "self")
            self = EmitSelfParameter();

        var args = node.Arguments.Select(EmitValue).ToArray();
        return self == null ? Generator.EmitCall(target, args) : Generator.EmitCallInstanced(target, self, args);
    }

    // References preserve the substituted signature until monomorphization supplies the body.
    private LIRValue GetCallTarget(MethodSymbol method) => methodGenerator.GetMethodReference(substitution?.Resolve(method) ?? method);
}
