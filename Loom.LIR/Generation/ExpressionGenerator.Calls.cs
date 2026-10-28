using Loom.Analyzer;
using Loom.Analyzer.Symbols;
using Loom.LIR.Objects;
using Loom.LIR.OpCodes;
using Loom.Parser.AST.Rules.Default;

namespace Loom.LIR.Generation;

internal partial class ExpressionGenerator
{
    private LIRValue EmitAccessorCall(MethodSymbol accessor, LIRValue? self, params LIRValue[] arguments) => EmitResolvedCall(GetCallTarget(accessor), self, arguments);

    private LIRValue EmitResolvedCall(LIRValue target, LIRValue? self, params LIRValue[] arguments)
    {
        if (target is LIRVirtualMethodReference method)
        {
            if (self?.Type is not LIRFatPointerType receiverType)
                throw new InvalidOperationException("An interface method requires a fat pointer receiver.");

            var instance = Generator.Emit(LIROpCode.ExtractValue, receiverType.FirstPointee, self, new LIRConstantIntValue(0));
            var table = Generator.Emit(LIROpCode.ExtractValue, receiverType.SecondPointee, self, new LIRConstantIntValue(1));
            var function = Generator.Emit(LIROpCode.GetVirtualMethod, new LIRPointerType(method.Type), table, new LIRConstantIntValue(method.Slot));
            return Generator.EmitCall(function, [instance, .. arguments]);
        }

        return self == null ? Generator.EmitCall(target, arguments) : Generator.EmitCallInstanced(target, self, arguments);
    }

    private LIRValue EmitIndexOperatorCall(ArrayAccessExpressionNode node)
    {
        var method = context.AnalysisContext.GetSymbol(node).As<MethodSymbol>()
            ?? throw new InvalidOperationException("Could not resolve operator[].");
        return Generator.EmitCall(GetCallTarget(method), EmitArguments([node.Receiver, node.Index], method));
    }

    private LIRValue EmitCall(CallExpressionNode node)
    {
        LIRValue? self = null;
        TypeSymbol? dispatchInterface = null;
        MethodSymbol methodSymbol = context.AnalysisContext.GetSymbol(node).As<MethodSymbol>() ?? throw new Exception($"Could not resolve call: {node.Callee}");

        if (node.Callee is MemberAccessExpressionNode member)
        {
            var receiverIsValue = context.AnalysisContext.ExpressionTypes.TryGetValue(member.Receiver, out var receiverType);
            if (!receiverIsValue)
                receiverType = context.AnalysisContext.GetSymbol(member.Receiver).Symbol as TypeSymbol;

            if (!receiverIsValue && !methodSymbol.IsStatic)
                throw new Exception($"Member '{member.Name.BaseName}' is not static and cannot be called on type '{receiverType?.Name}'.");

            if (receiverIsValue)
            {
                self = EmitAddress(member.Receiver);
                if (receiverType?.KnownType == TypeSymbol.DefaultType.Interface)
                    dispatchInterface = receiverType;
            }
        }

        var target = GetCallTarget(methodSymbol, dispatchInterface);

        if (self == null && ((LIRFunctionType)target.Type).Parameters.FirstOrDefault()?.Name == "self")
            self = EmitSelfParameter();

        var args = EmitArguments(node.Arguments, methodSymbol);
        return EmitResolvedCall(target, self, args);
    }

    private LIRValue[] EmitArguments(IReadOnlyList<ExpressionNode> arguments, MethodSymbol method) => [.. arguments.Select((argument, index) => EmitValue(argument, method.Parameters[index].Type!))];

    // References preserve the substituted signature until monomorphization supplies the body.
    private LIRValue GetCallTarget(MethodSymbol method, TypeSymbol? dispatchInterface = null) => methodGenerator.GetMethodReference(substitution?.Resolve(method) ?? method,
        dispatchInterface == null ? null : substitution?.Resolve(dispatchInterface) ?? dispatchInterface);
}
