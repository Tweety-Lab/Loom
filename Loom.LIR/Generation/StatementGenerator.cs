using Loom.Analyzer;
using Loom.Analyzer.Symbols;
using Loom.Common;
using Loom.LIR.Objects;
using Loom.Parser.AST;
using Loom.Parser.AST.Rules.Default;

namespace Loom.LIR.Generation;

internal class StatementGenerator
{
    private CompilationContext context;
    private LIRCompilationUnit unit;
    private LIRFunction function;
    private Dictionary<string, LIRTempValue> locals = new();

    protected LIRGenerator Generator => function.LIRGenerator!;

    /// <summary> Initializes a new instance of the <see cref="StatementGenerator"/> class. </summary>
    public StatementGenerator(CompilationContext context, LIRCompilationUnit unit, LIRFunction function)
    {
        this.context = context;
        this.unit = unit;
        this.function = function;

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
    public void EmitFieldInitializers(ASTNode node) => new ExpressionGenerator(context, unit, function, locals).EmitFieldInitializers(node);

    /// <summary> Emits a statement for the given node. </summary>
    public void EmitStatement(StatementNode node)
    {
        switch (node)
        {
            case ReturnStatementNode ret: EmitReturn(ret); break;
            case LocalDeclarationStatementNode decl: EmitLocalDeclaration(decl); break;
            case AssignmentStatementNode assign: EmitAssignment(assign); break;
            case IfStatementNode conditional: EmitIf(conditional); break;
            case WhileStatementNode loop: EmitWhile(loop); break;
            case ForStatementNode forLoop: EmitFor(forLoop); break;
            case ExpressionStatementNode expression: EmitExpression(expression.Expression); break;
        }
    }

    private LIRValue EmitExpression(ExpressionNode node) => new ExpressionGenerator(context, unit, function, locals).Emit(node);

    private LIRValue EmitValue(ExpressionNode node) => new ExpressionGenerator(context, unit, function, locals).EmitValue(node);

    private void EmitLocalDeclaration(LocalDeclarationStatementNode node)
    {
        var declaration = node.Variable;
        LocalVariableSymbol symbol = (LocalVariableSymbol)context.AnalysisContext.GetSymbol(node).Symbol!;

        LIRTempValue address = Generator.EmitAlloca(ASTGenerator.ConvertStorageType(symbol.Type!));
        locals[declaration.Name.Text] = address;

        if (declaration.Initializer is InstanceCreationExpresssionNode creation)
        {
            var expressionGen = new ExpressionGenerator(context, unit, function, locals);

            // The storage of a value type is the instance itself, so it is constructed in place; a reference type stores a pointer to it
            if (symbol.Type!.IsValueType)
            {
                expressionGen.EmitInstanceCreation(creation, address);
                return;
            }

            Generator.EmitStore(expressionGen.Emit(creation), address);
            return;
        }

        Initialize(address, declaration.Initializer, symbol.Type!);
    }

    /// <summary> Emits the initialization of <paramref name="address"/>. </summary>
    private void Initialize(LIRValue address, ExpressionNode initializer, TypeSymbol type)
        => new ExpressionGenerator(context, unit, function, locals).Initialize(address, initializer, type);

    private void EmitReturn(ReturnStatementNode node)
    {
        if (node.Expression == null)
            Generator.EmitReturn();
        else
            Generator.EmitReturn(EmitValue(node.Expression));
    }

    private void EmitAssignment(AssignmentStatementNode node)
    {
        var address = new ExpressionGenerator(context, unit, function, locals).EmitAddress(node.Target);

        // Assigning to a whole array defaults each of its elements rather than storing a single value
        if (context.AnalysisContext.ExpressionTypes.TryGetValue(node.Target, out var targetType) && targetType is ArrayTypeSymbol)
        {
            Initialize(address, node.Value, targetType);
            return;
        }

        Generator.EmitStore(EmitValue(node.Value), address);
    }

    private void EmitIf(IfStatementNode node)
    {
        var condition = EmitExpression(node.Expression);
        var thenBlock = Generator.CreateBlock("if.then");
        var continueBlock = Generator.CreateBlock("if.continue");

        Generator.EmitCondBr(condition, thenBlock, continueBlock);

        Generator.SwitchTo(thenBlock);
        foreach (var content in node.Body.Contents)
            if (content is StatementNode statementNode)
                EmitStatement(statementNode);

        // The body may already end control flow
        if (Generator.WritingBlock.Terminator == null)
            Generator.EmitBr(continueBlock);

        Generator.SwitchTo(continueBlock);
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

        foreach (var content in node.Body.Contents)
            if (content is StatementNode statementNode)
                EmitStatement(statementNode);

        if (Generator.WritingBlock.Terminator == null)
            Generator.EmitBr(conditionBlock);

        Generator.SwitchTo(continueBlock);
    }

    private void EmitFor(ForStatementNode node)
    {
        EmitStatement(node.Initializer);

        var conditionBlock = Generator.CreateBlock("for.condition");
        var bodyBlock = Generator.CreateBlock("for.body");

        var incrementorBlock = Generator.CreateBlock("for.increment");
        var continueBlock = Generator.CreateBlock("for.continue");

        Generator.EmitBr(conditionBlock);

        Generator.SwitchTo(conditionBlock);
        var condition = EmitExpression(node.Condition);
        Generator.EmitCondBr(condition, bodyBlock, incrementorBlock);

        Generator.SwitchTo(bodyBlock);

        foreach (var content in node.Body.Contents)
            if (content is StatementNode statementNode)
                EmitStatement(statementNode);

        if (Generator.WritingBlock.Terminator == null)
            Generator.EmitBr(incrementorBlock);

        Generator.SwitchTo(incrementorBlock);
        EmitStatement(node.Incrementor);

        if (Generator.WritingBlock.Terminator == null)
            Generator.EmitBr(conditionBlock);

        Generator.SwitchTo(continueBlock);
    }
}
