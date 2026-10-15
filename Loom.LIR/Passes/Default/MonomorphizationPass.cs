using Loom.Analyzer;
using Loom.Analyzer.Symbols;
using Loom.Common;
using Loom.LIR.Generation;
using Loom.LIR.Objects;
using Loom.LIR.OpCodes;
using Loom.Parser.AST;
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

        // Every instantiation of a generic type is emitted as its own concrete type before the members it is called through are monomorphized
        var typeGenerator = new TypeDeclarationGenerator(context, unit);

        foreach (InstantiatedTypeSymbol instance in context.AnalysisContext.Instantiations)
            typeGenerator.DeclareTypeDeclaration(instance, instance.Substitution);

        foreach (InstantiatedTypeSymbol instance in context.AnalysisContext.Instantiations)
            typeGenerator.GenerateTypeDeclarationBodies(instance, instance.Substitution);

        bool changed;
        do
        {
            changed = false;

            foreach (LIRFunction function in unit.AllFunctions.ToList())
            {
                foreach (LIRBasicBlock block in function.Blocks)
                {
                    foreach (LIRInstruction instruction in block.Instructions.ToList())
                        changed |= Monomorphize(methodGenerator, unit, instruction, context);
                }
            }
        }
        while (changed);
    }

    private static bool Monomorphize(MethodGenerator methodGenerator, LIRCompilationUnit unit, LIRInstruction call, CompilationContext context)
    {
        if (call.OpCode != LIROpCode.Call && call.OpCode != LIROpCode.CallInstanced)
            return false;

        if (call.Operands[0] is not LIRFunction target || target.Type.TypeParameters.Length == 0)
            return false;

        if (call.Origin is not ASTNode origin)
            return false;

        // A member of an instantiated generic type is redirected to the concrete function its type instantiation declared
        if (context.AnalysisContext.GetSymbol(origin).As<MethodSymbol>() is { GenericLinkageName: not null } member
            && target.Name != member.LinkageName)
        {
            call.Operands[0] = unit.GetFunction(member.LinkageName)
                ?? throw new InvalidOperationException($"Could not find the instantiated member '{member.LinkageName}'.");
            return true;
        }

        // A default constructor call is redirected to the constructor of the type the instance creation instantiates
        if (origin is InstanceCreationExpresssionNode expression
            && context.AnalysisContext.ExpressionTypes.TryGetValue(expression, out TypeSymbol? created)
            && created is InstantiatedTypeSymbol instanceType)
        {
            call.Operands[0] = unit.GetFunction($"{instanceType.FullyQualifiedName}::.ctor")
                ?? throw new InvalidOperationException($"Could not find the constructor of '{instanceType.FullyQualifiedName}'.");
            return true;
        }

        // A generic method call is monomorphized into the instance for the call's type arguments
        if (call.TypeArguments == null || origin is not CallExpressionNode callNode)
            return false;

        MethodSymbol method = context.AnalysisContext.GetSymbol(callNode).As<MethodSymbol>()
            ?? throw new InvalidOperationException($"Could not resolve the generic method called at '{callNode}'.");

        call.Operands[0] = methodGenerator.GetOrCreateInstance(method, call.TypeArguments, callNode);

        return true;
    }
}
