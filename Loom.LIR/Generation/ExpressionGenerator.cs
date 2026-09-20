using Loom.Analyzer;
using Loom.Analyzer.Symbols;
using Loom.Common;
using Loom.LIR.Objects;
using Loom.Parser.AST;
using Loom.Parser.Rules.Default;

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
        CharacterLiteralNode character => new LIRConstantCharValue(char.Parse(character.Value)),
        NumberLiteralNode num => new LIRConstantIntValue(int.Parse(num.Value)),
        IdentifierNameNode ident => Generator.EmitLoad(EmitAddress(ident)),
        BooleanLiteralNode boolean => new LIRConstantBoolValue(bool.Parse(boolean.Value)),
        DefaultLiteralNode @default => EmitDefault(context.AnalysisContext.ExpressionTypes[@default]),
        BinaryExpressionNode binary => EmitBinary(binary),
        CallExpressionNode call => EmitCall(call),
        MemberAccessExpressionNode member => Generator.EmitLoad(EmitAddress(member)),
        ObjectCreationExpressionNode creation => Generator.EmitAlloca(ASTGenerator.ConvertType(context.AnalysisContext.ExpressionTypes[creation])),
        _ => throw new Exception($"Unhandled expression: {node.GetType().Name}")
    };

    /// <summary> Emits the address of an addressable expression (a local variable or a field). </summary>
    public LIRValue EmitAddress(ExpressionNode node) => node switch
    {
        IdentifierNameNode ident => EmitIdentifierAddress(ident),
        MemberAccessExpressionNode member => EmitMemberAddress(member),
        ObjectCreationExpressionNode creation => Emit(creation),
        _ => throw new Exception($"Unhandled addressable expression: {node.GetType().Name}")
    };

    /// <summary> Emits <paramref name="node"/> as a value. </summary>
    public LIRValue EmitValue(ExpressionNode node)
    {
        var value = Emit(node);
        return node is ObjectCreationExpressionNode ? Generator.EmitLoad(value) : value;
    }

    public LIRValue EmitDefault(TypeSymbol type) => type.KnownType switch
    {
        TypeSymbol.DefaultType.Char => new LIRConstantCharValue('\0'),
        TypeSymbol.DefaultType.I32 => new LIRConstantIntValue(0),
        TypeSymbol.DefaultType.Bool => new LIRConstantBoolValue(false),
        TypeSymbol.DefaultType.IPtr => new LIRConstantIntValue(0),
        _ => throw new Exception($"Unhandled default value type: {type.KnownType}")
    };

    private LIRValue EmitIdentifierAddress(IdentifierNameNode ident)
    {
        if (locals.TryGetValue(ident.BaseName, out var address))
            return address;

        var symbol = context.AnalysisContext.GetSymbol(ident).Symbol;
        if (symbol is FieldSymbol fieldSymbol)
            return EmitBareFieldAddress(fieldSymbol, ident);

        throw new Exception($"Unhandled identifier: {ident.BaseName}");
    }

    private LIRValue EmitMemberAddress(MemberAccessExpressionNode node)
    {
        var receiverType = context.AnalysisContext.ExpressionTypes.TryGetValue(node.Receiver, out var type) ? type : null;
        if (receiverType == null)
            throw new Exception($"Could not resolve receiver type: {node.Receiver}");

        var fieldSymbol = receiverType.Members.OfType<FieldSymbol>().FirstOrDefault(m => m.Name == node.Name.BaseName)
            ?? throw new Exception($"Could not resolve member field: {node.Name.BaseName}");

        return EmitFieldAddress(EmitReceiverAddress(node.Receiver), receiverType, fieldSymbol);
    }

    /// <summary> Emits the address of a field referenced by bare name inside a method of the containing struct, accessed through the instance (self) parameter. </summary>
    private LIRValue EmitBareFieldAddress(FieldSymbol symbol, ASTNode contextNode)
    {
        var structNode = context.AnalysisContext.FirstAncestorOrSelf<StructDeclarationNode>(contextNode) ?? throw new Exception($"Could not find containing struct for field: {symbol.Name}");

        var structTypeSymbol = (TypeSymbol)context.AnalysisContext.GetSymbol(structNode).Symbol ?? throw new Exception($"Could not find symbol for struct: {structNode.Name}");

        return EmitFieldAddress(EmitSelfParameter(), structTypeSymbol, symbol);
    }

    /// <summary> Emits the value of the instance ('self') parameter of a struct method. </summary>
    private LIRValue EmitSelfParameter()
    {
        var index = Array.FindIndex(function.Type.Parameters, p => p.Name == "self");

        if (index < 0)
            throw new Exception($"Field access requires an instance method with a 'self' parameter ({function.Name}).");

        return function.ParameterValues[index];
    }

    /// <summary> Emits the address of <paramref name="fieldSymbol"/> within an instance of <paramref name="objectTypeSymbol"/>. </summary>
    private LIRValue EmitFieldAddress(LIRValue instance, TypeSymbol objectTypeSymbol, FieldSymbol fieldSymbol)
    {
        var structObj = unit.TypeDeclarations.FirstOrDefault(s => s.Type == new LIRTypeDeclarationType(objectTypeSymbol.FullyQualifiedName, true)) ?? throw new Exception($"Could not find struct: {objectTypeSymbol.FullyQualifiedName}");

        var field = structObj.Fields.FirstOrDefault(f => f.Name == fieldSymbol.Name) ?? throw new Exception($"Could not find field: {fieldSymbol.Name}");

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

            target = unit.AllFunctions.FirstOrDefault(f => f.Name == method.FullyQualifiedName);

            if (receiverIsValue)
                self = EmitReceiverAddress(member.Receiver);
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

        var args = node.Arguments.Select(EmitValue).ToArray();

        if (self != null)
            return Generator.EmitCallInstanced(target, self, args);

        return Generator.EmitCall(target, args);
    }

    /// <summary> Emits the address of <paramref name="receiver"/> to be passed as the instance (self) argument of a member call. </summary>
    private LIRValue EmitReceiverAddress(ExpressionNode receiver) => receiver switch
    {
        IdentifierNameNode ident => locals[ident.BaseName],
        ObjectCreationExpressionNode => Emit(receiver),
        _ => throw new Exception($"Unsupported member-access receiver: {receiver.GetType().Name}")
    };
}
