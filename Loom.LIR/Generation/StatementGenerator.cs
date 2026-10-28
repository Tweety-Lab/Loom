using Loom.Analyzer;
using Loom.Analyzer.Symbols;
using Loom.Common;
using Loom.LIR.Objects;
using Loom.Parser.AST;
using Loom.Parser.AST.Rules.Default;

namespace Loom.LIR.Generation;

internal partial class StatementGenerator
{
    protected Scope CurrentScope => scopes.Peek();
    protected LIRGenerator Generator => function.LIRGenerator!;

    private CompilationContext context;
    private LIRCompilationUnit unit;
    private LIRFunction function;
    private MethodGenerator methodGenerator;
    private TypeSubstitution? substitution;
    private TypeSymbol? ownerType;
    private Dictionary<string, LIRTempValue> locals = new();
    private readonly Stack<Scope> scopes = new();

    /// <summary> Initializes a new instance of the <see cref="StatementGenerator"/> class. </summary>
    public StatementGenerator(CompilationContext context, LIRCompilationUnit unit, LIRFunction function, MethodGenerator methodGenerator, TypeSubstitution? substitution = null, TypeSymbol? ownerType = null)
    {
        this.context = context;
        this.unit = unit;
        this.function = function;
        this.methodGenerator = methodGenerator;
        this.substitution = substitution;
        this.ownerType = ownerType;

        BeginScope();

        foreach (var (param, value) in function.Type.Parameters.Zip(function.ParameterValues))
        {
            // The instance pointer is consumed directly by field access; it needs no addressable slot
            if (param.Name == "self")
                continue;

            var address = Generator.EmitAlloca(param.Type);
            Generator.EmitStore(value, address);
            locals[param.Name] = address;
        }
    }

    /// <summary> Emits the declared initializer of every field of the type containing <paramref name="node"/>. </summary>
    public void EmitFieldInitializers(ASTNode node) => new ExpressionGenerator(context, unit, function, locals, methodGenerator, substitution, ownerType).EmitFieldInitializers(node);

    /// <summary> Emits a statement for the given node. </summary>
    public void EmitStatement(StatementNode node)
    {
        if (Generator.WritingBlock.Terminator != null)
            return;

        switch (node)
        {
            case ReturnStatementNode ret: EmitReturn(ret); break;
            case LocalDeclarationStatementNode decl: EmitLocalDeclaration(decl); break;
            case AssignmentStatementNode assign: EmitAssignment(assign); break;
            case IfStatementNode conditional: EmitIf(conditional); break;
            case UnsafeStatementNode unsafeStatement: EmitUnsafe(unsafeStatement); break;
            case WhileStatementNode loop: EmitWhile(loop); break;
            case ForStatementNode forLoop: EmitFor(forLoop); break;
            case ExpressionStatementNode expression: EmitExpression(expression.Expression); break;
        }
    }

    private LIRValue EmitExpression(ExpressionNode node) => new ExpressionGenerator(context, unit, function, locals, methodGenerator, substitution, ownerType).Emit(node);

    private LIRValue EmitValue(ExpressionNode node, TypeSymbol? targetType = null)
    {
        ExpressionGenerator expressions = new(context, unit, function, locals, methodGenerator, substitution, ownerType);

        return targetType == null ? expressions.EmitValue(node) : expressions.EmitValue(node, targetType);
    }

    private void EmitLocalDeclaration(LocalDeclarationStatementNode node)
    {
        var declaration = node.Variable;
        LocalVariableSymbol symbol = (LocalVariableSymbol)context.AnalysisContext.GetSymbol(node).Symbol!;
        TypeSymbol type = substitution?.Resolve(symbol.Type!) ?? symbol.Type!;

        LIRType storageType = ASTGenerator.ConvertStorageType(type, substitution);
        LIRTempValue address = Generator.EmitAlloca(storageType);
        CurrentScope.Bindings.TryAdd(symbol.Name, locals.GetValueOrDefault(symbol.Name));
        locals[symbol.Name] = address;

        if ((type.IsValueType && type.Members.OfType<MethodSymbol>().Any(method => method.Kind == MethodSymbol.MethodKind.Destructor)) || (symbol.PointerType == PointerType.Unique && storageType is LIRPointerType && !context.AnalysisContext.MovedValues.Contains(symbol)))
            CurrentScope.Owned.Add((address, type));

        if (declaration.Initializer is InstanceCreationExpresssionNode creation)
        {
            var expressionGen = new ExpressionGenerator(context, unit, function, locals, methodGenerator, substitution, ownerType);

            if (type.IsValueType)
            {
                expressionGen.EmitInstanceCreation(creation, address);
                return;
            }

            Generator.EmitStore(expressionGen.EmitValue(creation, type), address);
            return;
        }

        Initialize(address, declaration.Initializer, type);
    }

    /// <summary> Emits the initialization of <paramref name="address"/>. </summary>
    private void Initialize(LIRValue address, ExpressionNode initializer, TypeSymbol type) => new ExpressionGenerator(context, unit, function, locals, methodGenerator, substitution, ownerType).Initialize(address, initializer, type);

    private void EmitReturn(ReturnStatementNode node)
    {
        ASTNode? method = context.AnalysisContext.FirstAncestorOrSelf<MethodDeclarationNode>(node) ?? (ASTNode?)context.AnalysisContext.FirstAncestorOrSelf<OperatorDeclarationNode>(node) ?? context.AnalysisContext.FirstAncestorOrSelf<AccessorNode>(node);

        var returnType = method == null ? null : context.AnalysisContext.GetSymbol(method).As<MethodSymbol>()?.ReturnType;
        var result = node.Expression == null ? null : EmitValue(node.Expression, returnType);

        foreach (var scope in scopes)
            EmitCleanup(scope.Owned);

        Generator.EmitReturn(result);
    }

    private void EmitAssignment(AssignmentStatementNode node) => new ExpressionGenerator(context, unit, function, locals, methodGenerator, substitution, ownerType).EmitAssignment(node);
}
