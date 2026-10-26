using Loom.Analyzer.Symbols;
using Loom.Parser.AST.Rules.Default;

namespace Loom.Analyzer;

internal enum OverloadFailure
{
    NoMatchingOverload,
    Ambiguous
}

/// <summary>
/// How well the type of an argument matches the type of a parameter.
/// </summary>
/// <remarks> These values are ordered from weakest to strongest. </remarks>
internal enum MatchQuality
{
    /// <summary> The type of the argument or the parameter isn't known yet, so the parameter accepts anything. </summary>
    Unknown,

    /// <summary> The parameter accepts the argument through an implicit conversion. </summary>
    Implicit,

    /// <summary> The argument and the parameter are the same type. </summary>
    Exact
}

/// <summary>
/// Selects the method a call expression invokes from the set of methods sharing its name.
/// </summary>
internal static class OverloadResolver
{
    /// <summary> Finds addition operators declared on either operand type. </summary>
    public static IReadOnlyList<MethodSymbol> AdditionOperatorCandidates(AnalysisContext context, BinaryExpressionNode node) =>
        [.. new[] { node.Left, node.Right }
            .Select(operand => context.ExpressionTypes.GetValueOrDefault(operand))
            .OfType<TypeSymbol>()
            .Distinct()
            .SelectMany(type => type.Members.OfType<MethodSymbol>())
            .Where(method => method is { Kind: MethodSymbol.MethodKind.Operator, Name: ".+", IsStatic: true, Arity: 0 })
            .Distinct()];

    public static SymbolInfo ResolveAdditionOperator(AnalysisContext context, BinaryExpressionNode node)
    {
        var candidates = AdditionOperatorCandidates(context, node);
        var call = new CallExpressionNode(node.Left, new TypeArgumentListNode([]), [node.Left, node.Right]);
        var (best, tied) = Best(context, call, candidates);
        return best == null || tied ? SymbolInfo.OfCandidates(candidates) : SymbolInfo.OfCandidates(candidates).Resolved(best);
    }

    public static OverloadFailure? DiagnoseAdditionOperator(AnalysisContext context, BinaryExpressionNode node) => Diagnose(context, new CallExpressionNode(node.Left, new TypeArgumentListNode([]), [node.Left, node.Right]), AdditionOperatorCandidates(context, node));

    /// <summary> Finds static index operators on the receiver's type. </summary>
    public static IReadOnlyList<MethodSymbol> IndexOperatorCandidates(AnalysisContext context, ArrayAccessExpressionNode node) => context.ExpressionTypes.TryGetValue(node.Receiver, out var type) ? type.Members.OfType<MethodSymbol>().Where(method => method is { Kind: MethodSymbol.MethodKind.Operator, Name: ".[]", IsStatic: true, Arity: 0 }).ToArray() : [];

    /// <summary> Resolves receiver[index] as a static operator[](receiver, index) invocation. </summary>
    public static SymbolInfo ResolveIndexOperator(AnalysisContext context, ArrayAccessExpressionNode node)
    {
        var candidates = IndexOperatorCandidates(context, node);
        var call = new CallExpressionNode(node.Receiver, new TypeArgumentListNode([]), [node.Receiver, node.Index]);
        var (best, tied) = Best(context, call, candidates);
        return best == null || tied ? SymbolInfo.None : SymbolInfo.OfCandidates(candidates).Resolved(best);
    }

    /// <summary> Resolves <paramref name="node"/> against every method its callee could name. </summary>
    public static SymbolInfo Resolve(AnalysisContext context, CallExpressionNode node) => Resolve(context, node, Candidates(context, node.Callee));

    /// <summary> Resolves <paramref name="node"/> against an explicit set of <paramref name="candidates"/>. </summary>
    public static SymbolInfo Resolve(AnalysisContext context, CallExpressionNode node, IReadOnlyList<MethodSymbol> candidates)
    {
        var info = SymbolInfo.OfCandidates(candidates.Cast<Symbol>());

        if (candidates.Count == 0)
            return info;

        if (candidates.Count == 1)
            return info.Resolved(Construct(context, node, candidates[0]));

        var (best, tied) = Best(context, node, candidates);

        return best == null || tied ? info : info.Resolved(Construct(context, node, best));
    }

    /// <summary> Reports invalid or ambiguous static index operator invocations. </summary>
    public static OverloadFailure? DiagnoseIndexOperator(AnalysisContext context, ArrayAccessExpressionNode node) => Diagnose(context, new CallExpressionNode(node.Receiver, new TypeArgumentListNode([]), [node.Receiver, node.Index]), IndexOperatorCandidates(context, node));

    /// <summary> Reports why a call resolved to no method, or <see langword="null"/> when it resolved to one. </summary>
    public static OverloadFailure? Diagnose(AnalysisContext context, CallExpressionNode node) => Diagnose(context, node, Candidates(context, node.Callee));

    /// <summary> Reports why a call resolved to no method, or <see langword="null"/> when it resolved to one. </summary>
    public static OverloadFailure? Diagnose(AnalysisContext context, CallExpressionNode node, IReadOnlyList<MethodSymbol> candidates)
    {
        if (candidates.Count == 0)
            return null;

        if (candidates.Count == 1)
            return Match(context, node, candidates[0]) == null ? OverloadFailure.NoMatchingOverload : null;

        var (best, tied) = Best(context, node, candidates);

        if (best == null)
            return OverloadFailure.NoMatchingOverload;

        return tied ? OverloadFailure.Ambiguous : null;
    }

    private static IReadOnlyList<MethodSymbol> Candidates(AnalysisContext context, ExpressionNode callee) => context.GetSymbol(callee).AsMembers().AsAll<MethodSymbol>().ToArray();

    private static (MethodSymbol? Best, bool Tied) Best(AnalysisContext context, CallExpressionNode node, IReadOnlyList<MethodSymbol> candidates)
    {
        MethodSymbol? best = null;
        MatchQuality[]? bestMatches = null;
        var tied = false;

        foreach (var candidate in candidates)
        {
            var matches = Match(context, node, candidate);

            if (matches == null)
                continue;

            var comparison = bestMatches == null ? 1 : Compare(matches, bestMatches);

            if (comparison > 0)
            {
                best = candidate;
                bestMatches = matches;
                tied = false;
            }
            else if (comparison == 0)
            {
                tied = true;
            }
        }

        return (best, tied);
    }

    private static int Compare(MatchQuality[] left, MatchQuality[] right)
    {
        for (int index = 0; index < left.Length; index++)
        {
            var comparison = left[index].CompareTo(right[index]);

            if (comparison != 0)
                return comparison;
        }

        return 0;
    }

    private static MatchQuality[]? Match(AnalysisContext context, CallExpressionNode node, MethodSymbol candidate)
    {
        if (candidate.Arity != node.TypeArguments.Count)
            return null;

        if (candidate.Parameters.Count != node.Arguments.Count)
            return null;

        if (candidate.Parameters.Any(p => p.Type == null))
            return null;

        candidate = Construct(context, node, candidate);

        var matches = new MatchQuality[node.Arguments.Count];

        for (int index = 0; index < node.Arguments.Count; index++)
        {
            var parameter = candidate.Parameters[index].Type!;

            var argument = context.ExpressionTypes.TryGetValue(node.Arguments[index], out var argumentType) ? argumentType : null;

            if (argument == null || parameter.ContainsTypeParameters)
                matches[index] = MatchQuality.Unknown;

            else if (TypeConversions.IsIdentical(parameter, argument))
                matches[index] = MatchQuality.Exact;

            else if (TypeConversions.CanImplicitlyConvert(argument, parameter))
                matches[index] = MatchQuality.Implicit;

            else
                return null;
        }

        return matches;
    }

    private static MethodSymbol Construct(AnalysisContext context, CallExpressionNode node, MethodSymbol candidate)
    {
        if (node.TypeArguments.Count == 0 || node.TypeArguments.Count != candidate.Arity)
            return candidate;
        var arguments = node.TypeArguments.Select(t => context.GetSymbol(t).Symbol as TypeSymbol).ToArray();
        return arguments.Any(t => t == null) ? candidate : candidate.Construct([.. arguments!]);
    }
}
