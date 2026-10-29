using static Loom.Common.Diagnostics.DiagnosticDescription;

using Loom.Analyzer.Symbols;
using Loom.Common.Diagnostics;
using Loom.Parser.AST;
using Loom.Parser.AST.Rules.Default;

namespace Loom.Analyzer.Analyzers;

/// <summary>
/// Checks for invalid usage of pointer types.
/// </summary>
[LoomAnalyzer]
public class InvalidPointerTypeAnalyzer : Analyzer
{
    public static DiagnosticDescription ValueTypePointer = new($"LM{Code:D4}", Diagnostic.DiagnosticLevel.Error, "'{0}' pointer type cannot be applied to value type '{1}'.");
    public static DiagnosticDescription MissingPointer = new($"LM{Code:D4}", Diagnostic.DiagnosticLevel.Error, "Reference type '{0}' must declare a pointer type.");

    [Visitor]
    public void Visit(PropertyDeclarationNode node)
    {
        if (Context.GetSymbol(node).Symbol is not PropertySymbol { Accessibility: MemberAccessibility.Public, Type: { } type } property)
            return;

        if (type.IsValueType || type is ArrayTypeSymbol)
            return;

        if (property.PointerType == PointerType.None)
            Context.DiagnosticContext?.Report(MissingPointer, node.Name.Location, type.Name);
    }

    [Visitor]
    public void Visit(LocalDeclarationStatementNode node)
    {
        if (Context.GetSymbol(node).Symbol is not LocalVariableSymbol symbol || symbol.Type == null)
            return;

        if (symbol.Type.IsValueType && symbol.PointerType is not (PointerType.None or PointerType.Ref))
            Context.DiagnosticContext?.Report(ValueTypePointer, node.Variable.Span.StartToken?.Location, symbol.PointerType, symbol.Type.Name);

        if (symbol.Type is not ArrayTypeSymbol && !symbol.Type.IsValueType && symbol.PointerType == PointerType.None)
            Context.DiagnosticContext?.Report(MissingPointer, node.Variable.Span.StartToken?.Location, symbol.Type.Name);

    }

    [Visitor]
    public void Visit(MethodDeclarationNode node) => CheckReturnPointer(node);

    [Visitor]
    public void Visit(OperatorDeclarationNode node) => CheckReturnPointer(node);

    private void CheckReturnPointer(ASTNode node)
    {
        if (Context.GetSymbol(node).Symbol is not MethodSymbol symbol || symbol.ReturnType == null)
            return;

        if (symbol.ReturnType.KnownType == TypeSymbol.DefaultType.Void)
            return;

        if (!symbol.ReturnType.IsValueType && symbol.ReturnPointerType == PointerType.None)
            Context.DiagnosticContext?.Report(MissingPointer, node.Span.StartToken?.Location, symbol.ReturnType.Name);
    }
}
