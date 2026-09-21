
using Loom.Analyzer.Symbols;
using Loom.Common.Diagnostics;
using Loom.Parser.AST;
using Loom.Parser.Rules.Default;

namespace Loom.Analyzer.Analyzers;

/// <summary>
/// Checks for invalid usage of pointer types.
/// </summary>
[LoomAnalyzer]
public class InvalidPointerTypeAnalyzer : Analyzer
{
    public static Diagnostic ValueTypePointer = new(Diagnostic.DiagnosticLevel.Error, "'{0}' pointer type cannot be applied to value type '{1}'.");
    public static Diagnostic MissingPointer = new(Diagnostic.DiagnosticLevel.Error, "Reference type '{0}' must be initialized with a pointer type.");

    [Visitor]
    public void Visit(LocalDeclarationStatementNode node)
    {
        LocalVariableSymbol? symbol = Context.GetSymbol(node).Symbol as LocalVariableSymbol;
        if (symbol == null || symbol.Type == null)
            return;

        if (symbol.Type.IsValueType && symbol.PointerType != PointerType.None)
            Context.DiagnosticContext?.Report(ValueTypePointer, node.Variable.StartToken?.Location, symbol.PointerType, symbol.Type.Name);

        if (!symbol.Type.IsValueType && symbol.PointerType == PointerType.None)
            Context.DiagnosticContext?.Report(MissingPointer, node.Variable.StartToken?.Location, symbol.Type.Name);

    }
}