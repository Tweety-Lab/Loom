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

    private LIRGenerator Generator => function.LIRGenerator!;

    /// <summary> Initializes a new instance of the <see cref="ExpressionGenerator"/> class. </summary>
    public ExpressionGenerator(CompilationContext context, LIRCompilationUnit unit, LIRFunction function, Dictionary<string, LIRTempValue> locals)
    {
        this.context = context;
        this.unit = unit;
        this.function = function;
        this.locals = locals;
    }

    public LIRValue Emit(ExpressionNode node) => node switch
    {
        CharacterLiteralNode character => new LIRConstantCharValue(character.Value),
        NumberLiteralNode num => new LIRConstantIntValue(num.Value),
        IdentifierNameNode ident => EmitVariableValue(ident),
        BooleanLiteralNode boolean => new LIRConstantBoolValue(boolean.Value),
        DefaultLiteralNode @default => EmitDefault(context.AnalysisContext.ExpressionTypes[@default]),
        BinaryExpressionNode binary => EmitBinary(binary),
        CallExpressionNode call => EmitCall(call),
        ArrayAccessExpressionNode array => EmitVariableValue(array),
        MemberAccessExpressionNode member => EmitVariableValue(member),
        InstanceCreationExpressionNode creation => EmitInstanceCreation(creation),
        _ => throw new Exception($"Unhandled expression: {node.GetType().Name}")
    };

    /// <summary> Emits the address of an addressable expression (a local variable or a field). </summary>
    public LIRValue EmitAddress(ExpressionNode node) => node switch
    {
        IdentifierNameNode ident => EmitIdentifierAddress(ident),
        MemberAccessExpressionNode member => EmitMemberAddress(member),
        ArrayAccessExpressionNode array => EmitArrayElementAddress(array),
        InstanceCreationExpressionNode creation => Emit(creation),
        _ => throw new Exception($"Unhandled addressable expression: {node.GetType().Name}")
    };

    /// <summary> Emits <paramref name="node"/> as a value. </summary>
    public LIRValue EmitValue(ExpressionNode node)
    {
        var value = Emit(node);
        return node is InstanceCreationExpressionNode creation && context.AnalysisContext.ExpressionTypes[creation].IsValueType ? Generator.EmitLoad(value) : value;
    }

    /// <summary> Emits the default value of <paramref name="type"/>. </summary>
    public LIRValue EmitDefault(TypeSymbol type)
    {
        if (type is ArrayTypeSymbol)
            throw new Exception("An array has no single default value; each of its elements is defaulted individually through EmitDefaultArray.");

        if (!type.IsValueType)
            return new LIRNullValue(new LIRPointerType(ASTGenerator.ConvertType(type)));

        return type.KnownType switch
        {
            TypeSymbol.DefaultType.Char => new LIRConstantCharValue(0),
            TypeSymbol.DefaultType.I32 => new LIRConstantIntValue(0),
            TypeSymbol.DefaultType.Bool => new LIRConstantBoolValue(false),
            TypeSymbol.DefaultType.IPtr => new LIRConstantIntValue(0),
            _ => throw new Exception($"Unhandled default value type: {type.KnownType}")
        };
    }

    /// <summary> Allocates a new instance and calls its constructor. </summary>
    public LIRValue EmitInstanceCreation(InstanceCreationExpressionNode node)
    {
        Console.WriteLine($"Emitting instance creation: {node.TypeName.Token.Text}");

        var symbol = context.AnalysisContext.ExpressionTypes[node] ?? throw new Exception("Could not resolve instance creation type.");
        var instance = Generator.EmitAlloca(ASTGenerator.ConvertType(symbol));
        var constructor = symbol.Members.OfType<MethodSymbol>().FirstOrDefault(m => m.Kind == MethodSymbol.MethodKind.Constructor);

        LIRFunction? constructorFunc = null;
        if (constructor != null)
            constructorFunc = unit.GetFunction(constructor.FullyQualifiedName);
        else
            constructorFunc = unit.GetFunction(symbol.FullyQualifiedName + "::.ctor"); // Default

        if (constructorFunc == null)
            throw new Exception($"Could not find constructor for {symbol.FullyQualifiedName}!");


        var args = node.Arguments.Select(EmitValue).ToArray();
        Generator.EmitCallInstanced(constructorFunc, instance, args);

        return instance;
    }

    /// <summary> Emits a store of the default value of <paramref name="type"/> into every element of the array at <paramref name="arrayAddress"/>. </summary>
    public void EmitDefaultArray(LIRValue arrayAddress, ArrayTypeSymbol type)
    {
        for (int index = 0; index < type.Size; index++)
        {
            var elementAddress = Generator.EmitGetElement(arrayAddress, new LIRConstantIntValue(index));

            if (type.ElementType is ArrayTypeSymbol nested)
                EmitDefaultArray(elementAddress, nested);
            else
                Generator.EmitStore(EmitDefault(type.ElementType), elementAddress);
        }
    }

    // Emits a pointer to the storage of the local variable or field referenced by 'ident'.
    private LIRValue EmitStorage(IdentifierNameNode ident)
    {
        if (locals.TryGetValue(ident.BaseName, out var address))
            return address;

        var symbol = context.AnalysisContext.GetSymbol(ident).Symbol;
        if (symbol is FieldSymbol fieldSymbol)
            return EmitBareFieldAddress(fieldSymbol, ident);

        throw new Exception($"Unhandled identifier: {ident.BaseName}");
    }

    // Emits a pointer to the variable referenced by 'ident', which is the pointer it holds for a reference type.
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

    // Emits the value of the variable referenced by 'ident'.
    private LIRValue EmitVariableValue(IdentifierNameNode ident) => IsParameter(ident) ? EmitParameterValue(ident.BaseName) : Generator.EmitLoad(EmitStorage(ident));

    // Emits the value of the variable-like 'node', which is stored in memory.
    private LIRValue EmitVariableValue(ExpressionNode node)
    {
        var address = EmitAddress(node);
        return IsValueType(node) ? Generator.EmitLoad(address) : address;
    }

    // Determines whether 'ident' refers to a parameter rather than a local variable or a field.
    private bool IsParameter(IdentifierNameNode ident) => !locals.ContainsKey(ident.BaseName) && context.AnalysisContext.GetSymbol(ident).Symbol is ParameterSymbol;

    // Determines whether 'node' has a value type, defaulting to one when its type is unknown.
    private bool IsValueType(ExpressionNode node) => !context.AnalysisContext.ExpressionTypes.TryGetValue(node, out var type) || type.IsValueType;

    // Emits the value of the parameter named 'name'.
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

        var fieldSymbol = receiverType.Members.OfType<FieldSymbol>().FirstOrDefault(m => m.Name == node.Name.BaseName)
            ?? throw new Exception($"Could not resolve member field: {node.Name.BaseName}");

        return EmitFieldAddress(EmitAddress(node.Receiver), receiverType, fieldSymbol);
    }

    private LIRValue EmitArrayElementAddress(ArrayAccessExpressionNode node)
    {
        var arrayAddress = EmitAddress(node.Receiver);
        return Generator.EmitGetElement(arrayAddress, EmitValue(node.Index));
    }

    // Emits the address of a field referenced by bare name inside a type, accessed through the instance (self) parameter.
    private LIRValue EmitBareFieldAddress(FieldSymbol symbol, ASTNode contextNode)
    {
        var typeNode = context.AnalysisContext.FirstAncestorOrSelf<ITypeDeclarationNode>(contextNode);

        if (typeNode == null)
            throw new Exception($"Could not resolve field type: {symbol.Name}");

        var typeSymbol = (TypeSymbol?)context.AnalysisContext.GetSymbol((ASTNode)typeNode).Symbol;

        if (typeSymbol == null)
            throw new Exception($"Could not resolve field type: {symbol.Name}");

        return EmitFieldAddress(EmitSelfParameter(), typeSymbol, symbol);
    }

    // Emits the value of the instance (self) parameter.
    private LIRValue EmitSelfParameter()
    {
        if (!function.Type.Parameters.Any(p => p.Name == "self"))
            throw new Exception($"Field access requires an instance method with a 'self' parameter ({function.Name}).");

        return EmitParameterValue("self");
    }

    // Emits the address of a field referenced by bare name inside a type, accessed through the instance (self) parameter.
    private LIRValue EmitFieldAddress(LIRValue instance, TypeSymbol instanceTypeSymbol, FieldSymbol fieldSymbol)
    {
        var declarationType = new LIRTypeDeclarationType(instanceTypeSymbol.FullyQualifiedName, instanceTypeSymbol.IsValueType);
        var declaration = unit.TypeDeclarations.FirstOrDefault(s => s.Type == declarationType) ?? throw new Exception($"Could not find type: {instanceTypeSymbol.FullyQualifiedName}");

        var field = declaration.Fields.FirstOrDefault(f => f.Name == fieldSymbol.Name) ?? throw new Exception($"Could not find field: {fieldSymbol.Name}");

        return Generator.EmitGetField(instance, field);
    }

    private LIRValue EmitBinary(BinaryExpressionNode node)
    {
        var left = Emit(node.Left);
        var right = Emit(node.Right);

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
        LIRFunction? target = null;
        LIRValue? self = null;

        if (node.Callee is MemberAccessExpressionNode member)
        {
            var receiverIsValue = context.AnalysisContext.ExpressionTypes.TryGetValue(member.Receiver, out var receiverType);
            if (!receiverIsValue)
                receiverType = context.AnalysisContext.GetSymbol(member.Receiver).Symbol as TypeSymbol;

            var method = receiverType?.Members.OfType<MethodSymbol>().FirstOrDefault(m => m.Name == member.Name.BaseName);

            if (method == null)
                throw new Exception($"Could not resolve member call: {member.Name.BaseName}");

            if (!receiverIsValue && !method.IsStatic)
                throw new Exception($"Member '{member.Name.BaseName}' is not static and cannot be called on type '{receiverType?.Name}'.");

            target = unit.GetFunction(method.FullyQualifiedName);

            if (receiverIsValue)
                self = EmitAddress(member.Receiver);
        }
        else if (node.Callee is IdentifierNameNode ident)
        {
            var symbol = context.AnalysisContext.GetSymbol(ident).Symbol;
            if (symbol == null)
                throw new Exception($"Could not resolve call: {ident.Token.Text}");

            target = unit.GetFunction(symbol.FullyQualifiedName);
        }
        else
        {
            throw new Exception($"Unhandled call callee: {node.Callee.GetType().Name}");
        }

        if (target == null)
            throw new Exception($"Could not find function: {node.Callee}");

        // An unqualified call to an instance method dispatches on the current instance
        if (self == null && target.Type.Parameters.FirstOrDefault()?.Name == "self")
            self = EmitSelfParameter();

        var args = node.Arguments.Select(EmitValue).ToArray();

        if (self != null)
            return Generator.EmitCallInstanced(target, self, args);

        return Generator.EmitCall(target, args);
    }
}
