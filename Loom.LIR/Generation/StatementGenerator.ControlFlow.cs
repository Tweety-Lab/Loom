using Loom.Parser.AST.Rules.Default;

namespace Loom.LIR.Generation;

internal partial class StatementGenerator
{
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
