using Loom.Analyzer;
using Loom.Analyzer.Symbols;
using Loom.LIR.Objects;
using Loom.Parser.AST;
using Loom.Parser.AST.Rules.Default;

namespace Loom.LIR.Generation;

internal partial class ExpressionGenerator
{
    /// <summary> Emits the address of an addressable expression (a local variable or a field). </summary>
    public LIRValue EmitAddress(ExpressionNode node)
    {
        if (ResolveProperty(node) is { } property)
            return EmitPropertyAddress(property, node);

        if (node is ArrayAccessExpressionNode access && context.AnalysisContext.GetSymbol(access).Symbol is MethodSymbol { Kind: MethodSymbol.MethodKind.Operator })
        {
            var value = EmitIndexOperatorCall(access);
            if (value.Type is LIRPointerType)
                return value;

            var address = Generator.EmitAlloca(value.Type);
            Generator.EmitStore(value, address);
            return address;
        }

        return node switch
        {
            IdentifierNameNode ident => EmitIdentifierAddress(ident),
            MemberAccessExpressionNode or ArrayAccessExpressionNode => IsValueType(node) ? EmitStorage(node) : Generator.EmitLoad(EmitStorage(node)),
            ArrayLiteralNode literal => EmitArrayLiteral(literal),
            StringLiteralNode str => EmitStringLiteral(str),
            InstanceCreationExpresssionNode creation => Emit(creation),
            _ => throw new Exception($"Unhandled addressable expression: {node.GetType().Name}")
        };
    }

    // Assignment targets refer to their storage slot; reference receivers use the pointer stored in that slot.
    private LIRValue EmitStorage(ExpressionNode node) => node switch
    {
        IdentifierNameNode identifier => EmitStorage(identifier),
        MemberAccessExpressionNode member => EmitMemberStorage(member),
        ArrayAccessExpressionNode array => EmitArrayElementAddress(array),
        _ => throw new Exception($"Unhandled storage expression: {node.GetType().Name}")
    };

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

    // Reading a field or array element loads its stored value, including a reference pointer.
    private LIRValue EmitVariableValue(ExpressionNode node) => Generator.EmitLoad(EmitStorage(node));

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

    private LIRValue EmitParameterValue(string name)
    {
        var index = Array.FindIndex(function.Type.Parameters, p => p.Name == name);

        if (index < 0)
            throw new Exception($"Could not find parameter: {name}");

        return function.ParameterValues[index];
    }

    private LIRValue EmitMemberStorage(MemberAccessExpressionNode node)
    {
        var receiverType = context.AnalysisContext.ExpressionTypes.TryGetValue(node.Receiver, out var type) ? type : null;
        if (receiverType == null)
            throw new Exception($"Could not resolve receiver type: {node.Receiver}");

        var fieldSymbol = receiverType.Members.OfType<FieldSymbol>().FirstOrDefault(m => m.Name == node.Name.BaseName) ?? throw new Exception($"Could not resolve member field: {node.Name.BaseName}");

        return EmitFieldAddress(EmitAddress(node.Receiver), receiverType, fieldSymbol);
    }

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
            field = LIRField.Declare(field.Name, field.Index, ASTGenerator.ConvertStorageType(fieldSymbol.Type!, substitution));

        return Generator.EmitGetField(instance, field);
    }
}
