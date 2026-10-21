using Loom.Analyzer;
using Loom.Analyzer.Symbols;
using Loom.LIR.Intrinsics;
using Loom.LIR.Objects;
using Loom.Parser.AST;
using Loom.Parser.AST.Rules.Default;

namespace Loom.LIR.Generation;

internal partial class MethodGenerator
{
    /// <summary> Gets the instantiation of the generic method <paramref name="symbol"/> for its type arguments, generating it on first use. </summary>
    /// <param name="symbol"> The generic method to instantiate. </param>
    public LIRFunction GetOrCreateInstance(ConstructedMethodSymbol symbol)
    {
        var substitution = symbol.Substitution;
        string instanceName = LinkageNameResolver.LinkageName(symbol);

        LIRFunction? instance = unit.GetFunction(instanceName);
        if (instance != null)
            return instance;

        if (symbol.DeclaringNode is not MethodDeclarationNode node)
            throw new Exception($"Could not find the declaration of generic method {LinkageNameResolver.LinkageName(symbol)}.");

        var containingType = symbol.ContainingType;
        var owningType = containingType == null ? null : unit.GetTypeDeclaration(containingType.FullyQualifiedName);

        LIRType? selfType = owningType != null && !symbol.IsStatic ? new LIRPointerType(owningType.Type) : null;
        var functionType = ASTGenerator.BuildFunctionType(symbol, selfType, substitution);

        // Register the instance before generating its body so recursive calls resolve to it
        instance = owningType == null ? unit.DefineFunction(instanceName, functionType, !symbol.IsExtern) : symbol.IsExtern ? owningType.DeclareMethod(instanceName, functionType) : owningType.DefineMethod(instanceName, functionType);

        if (IIntrinsicEmitter.TryEmitIntrinsic(symbol, instance, unit))
            return instance;

        if (!symbol.IsExtern)
            GenerateBody(node, instance, substitution, containingType);

        return instance;
    }

    // The type parameters of the generic type enclosing 'node', or null when it is not declared inside a generic type
    private IReadOnlyList<TypeParameterSymbol>? GetOwningTypeParameters(ASTNode node)
    {
        var typeNode = context.AnalysisContext.FirstAncestorOrSelf<ITypeDeclarationNode>(node);

        return typeNode != null && context.AnalysisContext.GetSymbol((ASTNode)typeNode).Symbol is TypeSymbol { TypeParameters.Count: > 0 } owner
            ? owner.TypeParameters
            : null;
    }
}
