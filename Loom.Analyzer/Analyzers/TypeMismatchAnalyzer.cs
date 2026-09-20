using Loom.Analyzer.Symbols;
using Loom.Common.Diagnostics;
using Loom.Parser.AST;
using Loom.Parser.Rules.Default;

namespace Loom.Analyzer.Analyzers;

/// <summary>
/// Checks for type mismatches.
/// </summary>
[LoomAnalyzer]
public class TypeMismatchAnalyzer : Analyzer
{
    public static Diagnostic TypeMismatch = new(Diagnostic.DiagnosticLevel.Error, "Cannot implicitly cast '{0}' to expected type '{1}'.");
    public static Diagnostic UnexpectedValue = new(Diagnostic.DiagnosticLevel.Error, "Cannot return a value from a method that returns void.");
    public static Diagnostic MissingReturn = new(Diagnostic.DiagnosticLevel.Error, "Method with return type '{0}' must return a value.");

    [Visitor]
    public void Visit(ReturnStatementNode node)
    {
        var method = Context.FirstAncestorOrSelf<MethodDeclarationNode>(node);

        if (method == null)
            return;

        if (Context.GetSymbol(method).Symbol is not MethodSymbol methodSymbol)
            return;

        var returnType = methodSymbol.ReturnType;

        if (returnType == null)
            return;

        var isVoid = returnType.KnownType == TypeSymbol.DefaultType.Void;

        // return; when return is not void
        if (node.Expression == null)
        {
            if (!isVoid)
                Context.DiagnosticContext?.Report(MissingReturn, node.StartToken?.Location, returnType.Name);

            return;
        }

        // return value; when return is void
        if (isVoid)
        {
            Context.DiagnosticContext?.Report(UnexpectedValue, node.StartToken?.Location);
            return;
        }

        Check(returnType, node.Expression);
    }

    [Visitor]
    public void Visit(LocalDeclarationStatementNode node)
    {
        var localSymbol = Context.GetSymbol(node).Symbol as LocalVariableSymbol;
        if (localSymbol == null || localSymbol.Type == null)
            return;

        var initializer = node.Variable.Initializer;

        Check(localSymbol.Type, initializer);
    }

    [Visitor]
    public void Visit(FieldDeclarationNode node)
    {
        var fieldSymbol = Context.GetSymbol(node).Symbol as FieldSymbol;
        if (fieldSymbol == null || fieldSymbol.Type == null)
            return;

        var initializer = node.Variable.Initializer;

        Check(fieldSymbol.Type, initializer);
    }

    private void Check(TypeSymbol assignee, ExpressionNode assigned)
    {
        if (!Context.ExpressionTypes.TryGetValue(assigned, out var assignedType))
            return;

        if (!CanImplicitlyConvert(assignedType, assignee))
            Context.DiagnosticContext?.Report(TypeMismatch, assigned.StartToken?.Location, assignedType.Name, assignee.Name);
    }

    private static bool CanImplicitlyConvert(TypeSymbol source, TypeSymbol target)
    {
        if (source == null || target == null)
            return false;

        if (source.KnownType == target.KnownType)
            return true;

        if (target.KnownType == TypeSymbol.DefaultType.Void)
            return false;

        if (source.KnownType == TypeSymbol.DefaultType.Void)
            return false;

        // Casting check here

        return false;
    }
}
