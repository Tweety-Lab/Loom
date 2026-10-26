using Loom.Analyzer;
using Loom.Analyzer.Symbols;
using Loom.Common;
using Loom.LIR.Generation;
using Loom.LIR.Objects;

namespace Loom.LIR.Passes.Default;

[CompilationPass]
public class MonomorphizationPass : LIRCompilationPass
{
    /// <inheritdoc/>
    public override void Run(LIRCompilationUnit unit)
    {
        if (Context is not { } context)
            throw new InvalidOperationException($"{nameof(MonomorphizationPass)} requires a {nameof(CompilationContext)}.");

        new Specialization(context, unit).Run();
    }

    private sealed class Specialization(CompilationContext context, LIRCompilationUnit unit)
    {
        private readonly TypeDeclarationGenerator types = new(context, unit);
        private readonly MethodGenerator methods = new(context, unit);
        private readonly HashSet<ConstructedTypeSymbol> declaredTypes = [];
        private readonly HashSet<LIRFunction> processedFunctions = [];

        public void Run()
        {
            while (true)
            {
                foreach (var type in context.AnalysisContext.ConstructedTypes.Where(type => !type.ContainsTypeParameters))
                    DeclareType(type);

                var pending = unit.AllFunctions.Where(function => function.Type.TypeParameters.Length == 0 && processedFunctions.Add(function)).ToArray();

                if (pending.Length == 0)
                    return;

                foreach (var function in pending)
                    foreach (var instruction in function.Blocks.SelectMany(block => block.Instructions).ToArray())
                        RewriteFunctionReferences(instruction);
            }
        }

        private void DeclareType(ConstructedTypeSymbol type)
        {
            if (type.KnownType == TypeSymbol.DefaultType.Interface)
                return;

            if (!declaredTypes.Add(type))
                return;

            types.DeclareTypeDeclaration(type, type.Substitution);
            types.GenerateTypeDeclarationBodies(type, type.Substitution);
        }

        private void RewriteFunctionReferences(LIRInstruction instruction)
        {
            for (int index = 0; index < instruction.Operands.Count; index++)
                if (instruction.Operands[index] is LIRFunctionReference reference)
                    instruction.Operands[index] = ResolveFunction(reference);
        }

        private LIRFunction ResolveFunction(LIRFunctionReference reference)
        {
            switch (reference)
            {
                case LIRMethodReference { Method: { ContainsTypeParameters: false } method }:
                    if (method.ContainingType is ConstructedTypeSymbol containingType)
                        DeclareType(containingType);

                    if (method is ConstructedMethodSymbol constructed)
                        return methods.GetOrCreateInstance(constructed);

                    break;

                case LIRDefaultConstructorReference { DeclaringType: ConstructedTypeSymbol { ContainsTypeParameters: false } type }:
                    DeclareType(type);
                    break;

                default:
                    throw new InvalidOperationException($"Cannot materialize open function reference '{reference.Name}'.");
            }

            return unit.GetFunction(reference.Name) ?? throw new InvalidOperationException($"Missing LIR function '{reference.Name}'.");
        }
    }
}
