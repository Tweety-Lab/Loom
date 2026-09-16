using Loom.Analyzer.Symbols;
using Loom.Common.Diagnostics;
using Loom.Parser.AST;
using Loom.Parser.Rules.Default;

namespace Loom.Analyzer.Analyzers;

/// <summary>
/// Checks for usage of symbols that aren't exported.
/// </summary>
[LoomAnalyzer]
public class InaccessibleSymbolAnalyzer : Analyzer
{
    public static Diagnostic UnexportedSymbolDiagnostic = new(Diagnostic.DiagnosticLevel.Error, "The symbol '{0}' cannot be accessed as it is not exported.");

    [Visitor]
    public void Visit(CallExpressionNode node)
    {
        MethodSymbol? symbol = Context.GetSymbol(node.Callee).Symbol as MethodSymbol;

        if (symbol == null || symbol.DeclaringNode is not MethodDeclarationNode methodDecl)
            return;

        ModuleNode? callModule = Context.FirstAncestorOrSelf<ModuleNode>(node);
        ModuleNode? methodModule = Context.FirstAncestorOrSelf<ModuleNode>(methodDecl);

        if (callModule != methodModule && !methodDecl.HasModifier(Parser.Tokenizer.Token.TokenType.Export))
            Context.DiagnosticContext?.Report(UnexportedSymbolDiagnostic, symbol.Name);
    }
}
