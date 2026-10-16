using Loom.Analyzer;
using Loom.Analyzer.Symbols;
using Loom.Common;
using Loom.LIR.Objects;
using Loom.Parser.AST;
using Loom.Parser.AST.Rules.Default;

namespace Loom.LIR.Generation;

internal class StatementGenerator
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

    protected sealed class Scope
    {
        public List<(LIRValue Storage, TypeSymbol Type)> Owned { get; } = [];

        public Dictionary<string, LIRTempValue?> Bindings { get; } = [];
    }

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

    /// <summary> Opens a scope, collecting the unique values declared within it. </summary>
    public void BeginScope() => scopes.Push(new());

    /// <summary> Closes the current scope, destroying its owned values in reverse declaration order. </summary>
    public void EndScope()
    {
        var scope = scopes.Pop();

        if (Generator.WritingBlock.Terminator == null)
            EmitCleanup(scope.Owned);

        foreach (var (name, previous) in scope.Bindings)
            if (previous == null)
                locals.Remove(name);
            else
                locals[name] = previous;
    }

    private void EmitCleanup(IEnumerable<(LIRValue Storage, TypeSymbol Type)> owned)
    {
        foreach (var (storage, type) in owned.Reverse())
            methodGenerator.DestroyInstance(function, storage, type);
    }

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

    private LIRValue EmitValue(ExpressionNode node) => new ExpressionGenerator(context, unit, function, locals, methodGenerator, substitution, ownerType).EmitValue(node);

    private void EmitLocalDeclaration(LocalDeclarationStatementNode node)
    {
        var declaration = node.Variable;
        LocalVariableSymbol symbol = (LocalVariableSymbol)context.AnalysisContext.GetSymbol(node).Symbol!;
        TypeSymbol type = substitution?.Resolve(symbol.Type!) ?? symbol.Type!;

        LIRType storageType = ASTGenerator.ConvertStorageType(type, substitution);
        LIRTempValue address = Generator.EmitAlloca(storageType);
        CurrentScope.Bindings.TryAdd(symbol.Name, locals.GetValueOrDefault(symbol.Name));
        locals[symbol.Name] = address;

        // Value destructors operate on local storage; unique references additionally release their allocation
        if ((type.IsValueType && type.Members.OfType<MethodSymbol>().Any(method => method.Kind == MethodSymbol.MethodKind.Destructor)) ||
            (symbol.PointerType == PointerType.Unique && storageType is LIRPointerType && !context.AnalysisContext.MovedValues.Contains(symbol)))
            CurrentScope.Owned.Add((address, type));

        if (declaration.Initializer is InstanceCreationExpresssionNode creation)
        {
            var expressionGen = new ExpressionGenerator(context, unit, function, locals, methodGenerator, substitution, ownerType);

            // The storage of a value type is the instance itself, so it is constructed in place; a reference type stores a pointer to it
            if (type.IsValueType)
            {
                expressionGen.EmitInstanceCreation(creation, address);
                return;
            }

            Generator.EmitStore(expressionGen.Emit(creation), address);
            return;
        }

        Initialize(address, declaration.Initializer, type);
    }

    /// <summary> Emits the initialization of <paramref name="address"/>. </summary>
    private void Initialize(LIRValue address, ExpressionNode initializer, TypeSymbol type) => new ExpressionGenerator(context, unit, function, locals, methodGenerator, substitution, ownerType).Initialize(address, initializer, type);

    private void EmitReturn(ReturnStatementNode node)
    {
        var result = node.Expression == null ? null : EmitValue(node.Expression);

        foreach (var scope in scopes)
            EmitCleanup(scope.Owned);

        Generator.EmitReturn(result);
    }

    private void EmitAssignment(AssignmentStatementNode node) => new ExpressionGenerator(context, unit, function, locals, methodGenerator, substitution, ownerType).EmitAssignment(node);

    private void EmitIf(IfStatementNode node)
    {
        var condition = EmitExpression(node.Expression);
        var thenBlock = Generator.CreateBlock("if.then");
        var continueBlock = Generator.CreateBlock("if.continue");

        Generator.EmitCondBr(condition, thenBlock, continueBlock);

        Generator.SwitchTo(thenBlock);
        BeginScope();

        foreach (var content in node.Body.Contents)
            if (content is StatementNode statementNode)
                EmitStatement(statementNode);

        EndScope();

        // The body may already end control flow
        if (Generator.WritingBlock.Terminator == null)
            Generator.EmitBr(continueBlock);

        Generator.SwitchTo(continueBlock);
    }

    private void EmitUnsafe(UnsafeStatementNode node)
    {
        BeginScope();

        foreach (var content in node.Body.Contents)
            if (content is StatementNode statementNode)
                EmitStatement(statementNode);

        EndScope();
    }

    private void EmitWhile(WhileStatementNode node)
    {
        var conditionBlock = Generator.CreateBlock("while.condition");
        var bodyBlock = Generator.CreateBlock("while.body");
        var continueBlock = Generator.CreateBlock("while.continue");

        Generator.EmitBr(conditionBlock);

        Generator.SwitchTo(conditionBlock);
        var condition = EmitExpression(node.Expression);
        Generator.EmitCondBr(condition, bodyBlock, continueBlock);

        Generator.SwitchTo(bodyBlock);
        BeginScope();

        foreach (var content in node.Body.Contents)
            if (content is StatementNode statementNode)
                EmitStatement(statementNode);

        EndScope();

        if (Generator.WritingBlock.Terminator == null)
            Generator.EmitBr(conditionBlock);

        Generator.SwitchTo(continueBlock);
    }

    private void EmitFor(ForStatementNode node)
    {
        BeginScope();
        EmitStatement(node.Initializer);

        var conditionBlock = Generator.CreateBlock("for.condition");
        var bodyBlock = Generator.CreateBlock("for.body");

        var incrementorBlock = Generator.CreateBlock("for.increment");
        var continueBlock = Generator.CreateBlock("for.continue");

        Generator.EmitBr(conditionBlock);

        Generator.SwitchTo(conditionBlock);
        var condition = EmitExpression(node.Condition);
        Generator.EmitCondBr(condition, bodyBlock, continueBlock);

        Generator.SwitchTo(bodyBlock);
        BeginScope();

        foreach (var content in node.Body.Contents)
            if (content is StatementNode statementNode)
                EmitStatement(statementNode);

        EndScope();

        if (Generator.WritingBlock.Terminator == null)
            Generator.EmitBr(incrementorBlock);

        Generator.SwitchTo(incrementorBlock);
        EmitStatement(node.Incrementor);

        if (Generator.WritingBlock.Terminator == null)
            Generator.EmitBr(conditionBlock);

        Generator.SwitchTo(continueBlock);
        EndScope();
    }
}
