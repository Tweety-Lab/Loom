using static Loom.Common.Diagnostics.DiagnosticDescription;
using Loom.Common.Diagnostics;
using Loom.Analyzer.Symbols;
using Loom.Parser.AST;
using Loom.Parser.AST.Rules.Default;

namespace Loom.Analyzer.Analyzers;

/// <summary>
/// Checks that every call resolves to exactly one method.
/// </summary>
[LoomAnalyzer]
public class UnresolvedOverloadAnalyzer : Analyzer
{
    public static DiagnosticDescription AmbiguousOverload = new($"LM{Code:D4}", Diagnostic.DiagnosticLevel.Error, "The call to '{0}' is ambiguous.");
    public static DiagnosticDescription NoMatchingOverload = new($"LM{Code:D4}", Diagnostic.DiagnosticLevel.Error, "No overload of '{0}' accepts {1} argument(s).");

    [Visitor]
    public void Visit(BinaryExpressionNode node)
    {
        if (node.Operator.Type != Parser.Tokenizer.Token.TokenType.Plus)
            return;

        var failure = OverloadResolver.DiagnoseAdditionOperator(Context, node);
        if (failure == null && OverloadResolver.AdditionOperatorCandidates(Context, node).Count == 0
            && new[] { node.Left, node.Right }.Any(operand => Context.ExpressionTypes.GetValueOrDefault(operand)?.KnownType is TypeSymbol.DefaultType.Class or TypeSymbol.DefaultType.Struct))
            failure = OverloadFailure.NoMatchingOverload;

        if (failure is { } reason)
            Context.DiagnosticContext?.Report(reason == OverloadFailure.Ambiguous ? AmbiguousOverload : NoMatchingOverload,
                node.Operator.Location, "operator+", 2);
    }

    [Visitor]
    public void Visit(CallExpressionNode node)
    {
        if (OverloadResolver.Diagnose(Context, node) is not { } failure)
            return;

        var name = Name(node.Callee);

        if (failure == OverloadFailure.Ambiguous)
            Context.DiagnosticContext?.Report(AmbiguousOverload, node.Span.StartToken?.Location, name);
        else
            Context.DiagnosticContext?.Report(NoMatchingOverload, node.Span.StartToken?.Location, name, node.Arguments.Count);
    }

    [Visitor]
    public void Visit(InstanceCreationExpresssionNode node)
    {
        if (!Context.ExpressionTypes.TryGetValue(node, out var type) || type == null)
            return;

        var candidates = OverloadResolver.ConstructorCandidates(type, node);
        var call = new CallExpressionNode(node.TypeName, new TypeArgumentListNode([]), node.Arguments);
        var failure = OverloadResolver.Diagnose(Context, call, candidates);

        if (candidates.Count == 0 && (node.ConstructorName != null || node.Arguments.Count != 0 || type.Members.OfType<MethodSymbol>().Any(method => method.Kind == MethodSymbol.MethodKind.Constructor)))
            failure = OverloadFailure.NoMatchingOverload;

        if (failure is not { } reason)
            return;

        var name = node.ConstructorName is { } constructorName ? $"{node.TypeName.BaseName}:{constructorName.BaseName}" : node.TypeName.BaseName;
        Context.DiagnosticContext?.Report(reason == OverloadFailure.Ambiguous ? AmbiguousOverload : NoMatchingOverload, node.ConstructorName?.Token.Location ?? node.Span.StartToken?.Location, name, node.Arguments.Count);
    }

    private static string Name(ExpressionNode callee) => callee switch
    {
        IdentifierNameNode identifier => identifier.BaseName,
        MemberAccessExpressionNode member => member.Name.BaseName,
        _ => callee.GetType().Name
    };
}
