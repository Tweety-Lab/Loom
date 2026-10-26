using Loom.Analyzer;
using Loom.Analyzer.Symbols;
using Loom.Parser.AST.Rules.Default;

namespace Loom.LIR.Generation;

internal partial class ExpressionGenerator
{
    private LIRValue EmitMethodReference(ExpressionNode node, MethodSymbol method)
    {
        method = substitution?.Resolve(method) ?? method;
        if (method.Parameters.Count != 0 || method.Arity != 0 || method is { IsStatic: false, ContainingType: not null })
            throw new NotSupportedException("Standard::Function requires a method with no arguments or instance receiver.");

        var type = context.AnalysisContext.ExpressionTypes[node];
        var constructor = type.Members.OfType<MethodSymbol>().Single(method => method.Kind == MethodSymbol.MethodKind.Constructor && method.Parameters is [{ Type.KnownType: TypeSymbol.DefaultType.IPtr }]);
        var storage = Generator.EmitAlloca(ASTGenerator.ConvertType(type, substitution));
        var pointer = Generator.EmitPtrToInt(GetCallTarget(method));

        Generator.EmitCallInstanced(GetCallTarget(constructor), storage, pointer);

        return storage;
    }
}