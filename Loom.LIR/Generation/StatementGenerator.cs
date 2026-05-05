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

    protected LIRGenerator Generator => function.LIRGenerator;

    /// <summary> Initializes a new instance of the <see cref="StatementGenerator"/> class. </summary>
    public StatementGenerator(CompilationContext context, LIRCompilationUnit unit, LIRFunction function)
    {
        this.context = context;
        this.unit = unit;
        this.function = function;
    }

    private LIRValue ExpressionGenerator(ExpressionNode node) => new ExpressionGenerator(context, unit, function, locals).Emit(node);

    /// <summary> Emits a statement for the given node. </summary>
    public void EmitStatement(StatementNode node)
    {
        switch (node)
        {
            case ReturnStatementNode ret: EmitReturn(ret); break;
        }
    }

    private void EmitReturn(ReturnStatementNode node)
    {
        if (node.Expression == null)
            Generator.EmitReturn();
        else
            Generator.EmitReturn(ExpressionGenerator(node.Expression));
    }
}
