using Loom.Analyzer.Symbols;
using Loom.LIR.Intrinsics;

namespace Loom.LIR.Generation;

public static class LinkageNameResolver
{
    /// <summary> The name that uniquely identifies <paramref name="symbol"/> in generated code. </summary>
    public static string LinkageName(Symbol symbol)
    {
        if (symbol is not MethodSymbol method)
            return symbol.FullyQualifiedName;

        // Intrinsics have compiler-generated bodies and need distinct names
        if (method.IsExtern && !IIntrinsicEmitter.HasIntrinsicEmitter(method))
            return method.Name;

        string arity = method.IsGeneric ? $"`{method.Arity}" : string.Empty;
        string name = $"{method.FullyQualifiedName}{arity}{Signature(method.OriginalDefinition)}";

        return method is ConstructedMethodSymbol ? $"{name}<{string.Join(", ", method.TypeArguments.Select(argument => argument.FullyQualifiedName))}>" : name;
    }

    /// <summary> The mangled parameter list that distinguishes an overloaded method from its siblings. </summary>
    public static string Signature(MethodSymbol method) => $".{string.Join("_", method.Parameters.Select(parameter => parameter.Type?.FullyQualifiedName ?? parameter.Name))}";
}
