using Loom.Analyzer;
using Loom.Analyzer.Symbols;
using Loom.Common;
using Loom.LIR.Objects;
using Loom.Parser.Rules.Default;

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
            // The instance pointer is consumed directly by field access; it needs no addressable slot.
            if (param.Name == "self")
                continue;

            var address = Generator.EmitAlloca(param.Type);
            Generator.EmitStore(value, address);
            locals[param.Name] = address;
        }
    }

    /// <summary> Emits a statement for the given node. </summary>
    public void EmitStatement(StatementNode node)
    {
        switch (node)
        {
            case ReturnStatementNode ret: EmitReturn(ret); break;
            case LocalDeclarationStatementNode decl: EmitLocalDeclaration(decl); break;
            case AssignmentStatementNode assign: EmitAssignment(assign); break;
            case ConditionalNode conditional: EmitConditional(conditional); break;
            case ExpressionStatementNode expression: EmitExpression(expression.Expression); break;
        }
    }

    private LIRValue EmitExpression(ExpressionNode node) => new ExpressionGenerator(context, unit, function, locals).Emit(node);

    private void EmitLocalDeclaration(LocalDeclarationStatementNode node)
    {
        var declaration = node.Variable;
        LocalVariableSymbol symbol = (LocalVariableSymbol)context.AnalysisContext.GetSymbol(node).Symbol!;
        var type = ASTGenerator.ConvertType(symbol.Type);

        if (type is LIRStructType && declaration.Initializer is ObjectCreationExpressionNode)
        {
            locals[declaration.Name.Text] = Generator.EmitAlloca(type);
            return;
        }

        if (type is LIRStructType)
            type = new LIRPointerType(type);

        LIRTempValue address = Generator.EmitAlloca(type);
        locals[declaration.Name.Text] = address;

        var value = EmitExpression(declaration.Initializer);
        Generator.EmitStore(value, address);
    }

    private void EmitReturn(ReturnStatementNode node)
    {
        if (node.Expression == null)
            Generator.EmitReturn();
        else
            Generator.EmitReturn(EmitExpression(node.Expression));
    }

    private void EmitAssignment(AssignmentStatementNode node)
    {
        LocalVariableSymbol symbol = (LocalVariableSymbol)context.AnalysisContext.GetSymbol(node.Target).Symbol!;
        if (!locals.TryGetValue(symbol.Name, out var address))
            throw new Exception($"Undeclared variable: {symbol.Name}");

        Generator.EmitStore(EmitExpression(node.Value), address);
    }

    private void EmitConditional(ConditionalNode node)
    {
        var condition = EmitExpression(node.Expression);
        var thenBlock = Generator.CreateBlock("if.then");
        var continueBlock = Generator.CreateBlock("if.continue");

        Generator.EmitCondBr(condition, thenBlock, continueBlock);

        Generator.SwitchTo(thenBlock);
        foreach (var content in node.Body.Contents)
            if (content is StatementNode statementNode)
                EmitStatement(statementNode);

        Generator.EmitBr(continueBlock);

        Generator.SwitchTo(continueBlock);
    }
}
