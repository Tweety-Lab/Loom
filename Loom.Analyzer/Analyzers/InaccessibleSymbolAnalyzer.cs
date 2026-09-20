using Loom.Analyzer.Symbols;
using Loom.Common.Diagnostics;
using Loom.Parser.AST;
using Loom.Parser.Rules.Default;

namespace Loom.Analyzer.Analyzers;

/// <summary>
/// Checks for usage of symbols that aren't exported or members that are private.
/// </summary>
[LoomAnalyzer]
public class InaccessibleSymbolAnalyzer : Analyzer
{
    public static Diagnostic UnexportedSymbolDiagnostic = new(Diagnostic.DiagnosticLevel.Error, "The symbol '{0}' cannot be accessed as it is not exported.");
    public static Diagnostic InaccessibleMemberDiagnostic = new(Diagnostic.DiagnosticLevel.Error, "The member '{0}' cannot be accessed as it is not public.");

    [Visitor]
    public void Visit(IdentifierNameNode node)
    {
        var symbol = Context.GetSymbol(node).Symbol;

        if (symbol is null or ModuleSymbol)
            return;

        CheckAccessible(node, symbol);
    }

    [Visitor]
    public void Visit(MemberAccessExpressionNode node)
    {
        var receiverType = Context.ExpressionTypes.TryGetValue(node.Receiver, out var type) ? type : Context.GetSymbol(node.Receiver).Symbol as TypeSymbol;

        if (receiverType?.Members.FirstOrDefault(m => m.Name == node.Name.BaseName) is not Symbol member)
            return;

        MemberAccessibility? accessibility = (member as MethodSymbol)?.Accessibility ?? (member as FieldSymbol)?.Accessibility;
        if (accessibility != MemberAccessibility.Private)
            return;

        var declaringStruct = Context.FirstAncestorOrSelf<StructDeclarationNode>(member.DeclaringNode!);
        var usageStruct = Context.FirstAncestorOrSelf<StructDeclarationNode>(node);

        if (declaringStruct != null && declaringStruct != usageStruct)
            Context.DiagnosticContext?.Report(InaccessibleMemberDiagnostic, member.Name);
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

        if (symbol is not IExportable exportable)
            return;

        if (usageModule != symbolModule && !exportable.IsExported)
            Context.DiagnosticContext?.Report(UnexportedSymbolDiagnostic, symbol.Name);
    }
}