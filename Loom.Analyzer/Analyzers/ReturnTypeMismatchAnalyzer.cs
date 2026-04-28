using Loom.Analyzer.Symbols;
using Loom.Common.Diagnostics;
using Loom.Parser.AST;
using Loom.Parser.Rules.Default;

namespace Loom.Analyzer.Analyzers;

/// <summary>
/// Checks for return type mismatches in methods.
/// </summary>
[LoomAnalyzer]
public class ReturnTypeMismatchAnalyzer : Analyzer
{
    public static Diagnostic ReturnTypeMismatch = new(Diagnostic.DiagnosticLevel.Error, "Cannot implicitly cast '{0}' to expected type '{1}'.");
    public static Diagnostic UnexpectedValue = new(Diagnostic.DiagnosticLevel.Error, "Cannot return a value from a method that returns void.");
    public static Diagnostic MissingReturn = new(Diagnostic.DiagnosticLevel.Error, "Method with return type '{0}' must return a value.");

    [Visitor]
    public void Visit(ReturnStatementNode node)
    {
        var method = Context.FirstAncestorOrSelf<MethodDefinitionNode>(node);

        if (method == null)
            return;

        var methodSymbol = Context.FirstAncestorOrSelf<ModuleNode>(method) is { } module ? Context.ResolveSymbol(module, method.MethodName.BaseName) as MethodDefinitionSymbol : null;

        if (methodSymbol == null)
            return;

        var isVoid = methodSymbol.ReturnType.Type == TypeSymbol.KnownType.Void;

        if (node.Expression == null && !isVoid)
            Context.DiagnosticContext?.Report(MissingReturn, methodSymbol.ReturnType.Name);
        else if (node.Expression != null && isVoid)
            Context.DiagnosticContext?.Report(UnexpectedValue);
        else if (node.Expression != null && Context.ExpressionTypes.TryGetValue(node.Expression, out var exprType))
            if (exprType.Type != methodSymbol.ReturnType.Type)
                Context.DiagnosticContext?.Report(ReturnTypeMismatch, exprType.Name, methodSymbol.ReturnType.Name);
    }
}
