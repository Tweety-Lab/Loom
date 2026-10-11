using Loom.Analyzer;
using Loom.Analyzer.Symbols;
using Loom.Common;
using Loom.LIR.Generation;
using Loom.LIR.Objects;
using Loom.LIR.OpCodes;
using Loom.Parser.AST.Rules.Default;

namespace Loom.LIR.Passes.Default;


[CompilationPass]
public class MonomorphizationPass : LIRCompilationPass
{
    /// <inheritdoc/>
    public override void Run(LIRCompilationUnit unit)
    {
        if (Context == null)
            throw new InvalidOperationException($"{nameof(MonomorphizationPass)} requires a {nameof(CompilationContext)} to be set.");

        CompilationContext context = Context;
        MethodGenerator methodGenerator = new(context, unit);

        bool changed;
        do
        {
            changed = false;

            foreach (LIRFunction function in unit.AllFunctions.ToList())
            {
                foreach (LIRBasicBlock block in function.Blocks)
                {
                    foreach (LIRInstruction instruction in block.Instructions.ToList())
                        changed |= Monomorphize(methodGenerator, instruction, context);
                }
            }
        }
        while (changed);
    }

    private static bool Monomorphize(MethodGenerator methodGenerator, LIRInstruction call, CompilationContext context)
    {
        if (call.OpCode != LIROpCode.Call && call.OpCode != LIROpCode.CallInstanced)
            return false;

        if (call.Operands[0] is not LIRFunction target || target.Type.TypeParameters.Length == 0)
            return false;

        if (call.TypeArguments == null || call.Origin is not CallExpressionNode origin)
            return false;

        MethodSymbol method = context.AnalysisContext.GetSymbol(origin).As<MethodSymbol>() ?? throw new InvalidOperationException($"Could not resolve the generic method called at '{origin}'.");

        LIRFunction instance = methodGenerator.GetOrCreateInstance(method, call.TypeArguments, origin);

        call.Operands[0] = instance;

        return true;
    }
}