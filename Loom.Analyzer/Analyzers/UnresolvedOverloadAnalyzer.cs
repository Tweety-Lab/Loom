using Loom.Common.Diagnostics;
using Loom.Parser.AST;
using Loom.Parser.AST.Rules.Default;

namespace Loom.Analyzer.Analyzers;

/// <summary>
/// Checks that every call resolves to exactly one method.
/// </summary>
[LoomAnalyzer]
public class UnresolvedOverloadAnalyzer : Analyzer
{
    public static Diagnostic AmbiguousOverload = new(Diagnostic.DiagnosticLevel.Error, "The call to '{0}' is ambiguous.");
    public static Diagnostic NoMatchingOverload = new(Diagnostic.DiagnosticLevel.Error, "No overload of '{0}' accepts {1} argument(s).");

    [Visitor]
    public void Visit(CallExpressionNode node)
    {
        if (OverloadResolver.Diagnose(Context, node) is not { } failure)
            return;

        var name = Name(node.Callee);

        if (failure == OverloadFailure.Ambiguous)
            Context.DiagnosticContext?.Report(AmbiguousOverload, node.StartToken?.Location, name);
        else
            Context.DiagnosticContext?.Report(NoMatchingOverload, node.StartToken?.Location, name, node.Arguments.Count);
    }

    private static string Name(ExpressionNode callee) => callee switch
    {
        IdentifierNameNode identifier => identifier.BaseName,
        MemberAccessExpressionNode member => member.Name.BaseName,
        _ => callee.GetType().Name
    };
}
