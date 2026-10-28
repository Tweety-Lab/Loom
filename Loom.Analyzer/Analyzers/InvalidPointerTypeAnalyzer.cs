
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
    public static DiagnosticDescription ValueTypePointer = new("LM0018", Diagnostic.DiagnosticLevel.Error, "'{0}' pointer type cannot be applied to value type '{1}'.");
    public static DiagnosticDescription MissingPointer = new("LM0019", Diagnostic.DiagnosticLevel.Error, "Reference type '{0}' must be initialized with a pointer type.");
    public static DiagnosticDescription MissingReturnPointer = new("LM0020", Diagnostic.DiagnosticLevel.Error, "Return reference type '{0}' must have a pointer type.");

    [Visitor]
    public void Visit(LocalDeclarationStatementNode node)
    {
        if (Context.GetSymbol(node).Symbol is not LocalVariableSymbol symbol || symbol.Type == null)
            return;

        if (symbol.Type.ContainsTypeParameters)
            return;

        if (symbol.Type.IsValueType && symbol.PointerType != PointerType.None)
            Context.DiagnosticContext?.Report(ValueTypePointer, node.Variable.Span.StartToken?.Location, symbol.PointerType, symbol.Type.Name);

        if (symbol.Type is not ArrayTypeSymbol && !symbol.Type.IsValueType && symbol.PointerType == PointerType.None)
            Context.DiagnosticContext?.Report(MissingPointer, node.Variable.Span.StartToken?.Location, symbol.Type.Name);

    }

    [Visitor]
    public void Visit(MethodDeclarationNode node)
    {
        if (Context.GetSymbol(node).Symbol is not MethodSymbol symbol || symbol.ReturnType == null)
            return;

        if (symbol.ReturnType.KnownType == TypeSymbol.DefaultType.Void)
            return;

        if (!symbol.ReturnType.IsValueType && symbol.ReturnPointerType == PointerType.None)
            Context.DiagnosticContext?.Report(MissingReturnPointer, node.Span.StartToken?.Location, symbol.ReturnType.Name);
    }
}
