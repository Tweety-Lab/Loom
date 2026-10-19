using Loom.Analyzer;
using Loom.Analyzer.Symbols;
using Loom.Parser.AST.Rules.Default;

namespace Loom.LIR.Generation;

internal partial class ExpressionGenerator
{
    private LIRValue EmitAccessorCall(MethodSymbol accessor, LIRValue? self, params LIRValue[] arguments) => self == null ? Generator.EmitCall(GetCallTarget(accessor), arguments) : Generator.EmitCallInstanced(GetCallTarget(accessor), self, arguments);

    private LIRValue EmitIndexOperatorCall(ArrayAccessExpressionNode node)
    {
        var method = context.AnalysisContext.GetSymbol(node).As<MethodSymbol>()
            ?? throw new InvalidOperationException("Could not resolve operator[].");
        return Generator.EmitCall(GetCallTarget(method), EmitArguments([node.Receiver, node.Index], method));
    }

    private LIRValue EmitCall(CallExpressionNode node)
    {
        LIRValue? self = null;
        MethodSymbol methodSymbol = context.AnalysisContext.GetSymbol(node).As<MethodSymbol>() ?? throw new Exception($"Could not resolve call: {node.Callee}");

        if (node.Callee is MemberAccessExpressionNode member)
        {
            var receiverIsValue = context.AnalysisContext.ExpressionTypes.TryGetValue(member.Receiver, out var receiverType);
            if (!receiverIsValue)
                receiverType = context.AnalysisContext.GetSymbol(member.Receiver).Symbol as TypeSymbol;

            if (!receiverIsValue && !methodSymbol.IsStatic)
                throw new Exception($"Member '{member.Name.BaseName}' is not static and cannot be called on type '{receiverType?.Name}'.");

            if (receiverIsValue)
                self = EmitAddress(member.Receiver);
        }

        var target = GetCallTarget(methodSymbol);

        if (self == null && ((LIRFunctionType)target.Type).Parameters.FirstOrDefault()?.Name == "self")
            self = EmitSelfParameter();

        var args = EmitArguments(node.Arguments, methodSymbol);
        return self == null ? Generator.EmitCall(target, args) : Generator.EmitCallInstanced(target, self, args);
    }

    private LIRValue[] EmitArguments(IReadOnlyList<ExpressionNode> arguments, MethodSymbol method) => [.. arguments.Select((argument, index) => EmitValue(argument, method.Parameters[index].Type!))];

    // References preserve the substituted signature until monomorphization supplies the body.
    private LIRValue GetCallTarget(MethodSymbol method) => methodGenerator.GetMethodReference(substitution?.Resolve(method) ?? method);
}
