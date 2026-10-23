using Loom.Analyzer.Symbols;
using Loom.Common.Diagnostics;
using Loom.Parser.AST;
using Loom.Parser.AST.Rules.Default;

namespace Loom.Analyzer.Analyzers;

/// <summary>
/// Checks for usage of unsafe code outside of <c>unsafe</c> blocks.
/// </summary>
[LoomAnalyzer]
public class UnsafeCodeAnalyzer : Analyzer
{
    public static Diagnostic SymbolUsedOutsideUnsafe = new(Diagnostic.DiagnosticLevel.Error, "Symbol '{0}' cannot be used outside of an 'unsafe' block.");

    [Visitor]
    public void Visit(CallExpressionNode node)
    {
        if (node.Callee is MemberAccessExpressionNode member)
        {
            Symbol? symbol = Context.GetSymbol(member.Receiver).Symbol;
            if (symbol == null)
                return;

            if (symbol.FullyQualifiedName.StartsWith("Standard::Memory::Unsafe") && Context.FirstAncestorOrSelf<UnsafeStatementNode>(node) == null)
                Context.DiagnosticContext?.Report(SymbolUsedOutsideUnsafe, node.Span.StartToken?.Location, symbol.FullyQualifiedName);
        }
    }
}
