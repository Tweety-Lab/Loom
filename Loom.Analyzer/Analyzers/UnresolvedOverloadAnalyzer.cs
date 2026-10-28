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
    public static DiagnosticDescription AmbiguousOverload = new("LM0036", Diagnostic.DiagnosticLevel.Error, "The call to '{0}' is ambiguous.");
    public static DiagnosticDescription NoMatchingOverload = new("LM0037", Diagnostic.DiagnosticLevel.Error, "No overload of '{0}' accepts {1} argument(s).");

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

    private static string Name(ExpressionNode callee) => callee switch
    {
        IdentifierNameNode identifier => identifier.BaseName,
        MemberAccessExpressionNode member => member.Name.BaseName,
        _ => callee.GetType().Name
    };
}
