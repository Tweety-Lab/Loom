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
    public void Visit(IdentifierNameNode node)
    {
        var symbol = Context.GetSymbol(node).Symbol;

        if (symbol is null or ModuleSymbol)
            return;

        CheckAccessible(node, symbol);
    }

    [Visitor]
    public void Visit(LocalDeclarationStatementNode node) => CheckTypeAccess(node, node.Variable.Type.Text);

    [Visitor]
    public void Visit(FieldDeclarationNode node) => CheckTypeAccess(node, node.Variable.Type.Text);

    [Visitor]
    public void Visit(MethodDeclarationNode node)
    {
        CheckTypeAccess(node, node.ReturnType.Text);

        foreach (var parameter in node.Parameters)
            CheckTypeAccess(node, parameter.Type.Text);
    }

    private void CheckTypeAccess(ASTNode node, string typeName)
    {
        if (TypeResolver.Resolve(Context, node, typeName) is { } type)
            CheckAccessible(node, type);
    }

    private void CheckAccessible(ASTNode node, Symbol symbol)
    {
        if (symbol.DeclaringNode == null)
            return;

        ModuleNode? usageModule = Context.FirstAncestorOrSelf<ModuleNode>(node);
        ModuleNode? symbolModule = Context.FirstAncestorOrSelf<ModuleNode>(symbol.DeclaringNode);

        if (usageModule != symbolModule && !IsExported(symbol))
            Context.DiagnosticContext?.Report(UnexportedSymbolDiagnostic, symbol.Name);
    }

    private static bool IsExported(Symbol symbol) => symbol switch
    {
        MethodSymbol method => method.IsExported,
        TypeSymbol type => type.IsExported,
        _ => true
    };
}