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
internal partial class ExpressionGenerator
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
        ArrayAccessExpressionNode array => context.AnalysisContext.GetSymbol(array).Symbol is MethodSymbol { Kind: MethodSymbol.MethodKind.Operator } ? EmitIndexOperatorCall(array) : EmitVariableValue(array),
        MemberAccessExpressionNode member => ResolveProperty(member) is { } memberProperty ? EmitPropertyGet(memberProperty, member, EmitPropertySelf(memberProperty, member)) : EmitVariableValue(member),
        InstanceCreationExpresssionNode creation => EmitInstanceCreation(creation),
        SizeOfExpressionNode sizeOf => EmitSizeOf(sizeOf),
        IncrementExpressionNode increment => EmitIncrement(increment),
        _ => throw new Exception($"Unhandled expression: {node.GetType().Name}")
    };

    /// <summary> Emits <paramref name="node"/> as a value. </summary>
    public LIRValue EmitValue(ExpressionNode node)
    {
        var value = Emit(node);
        return value.Type is LIRPointerType pointer && IsValueType(pointer.PointeeType) ? Generator.EmitLoad(value) : value;
    }
}
